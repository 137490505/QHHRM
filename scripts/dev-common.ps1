Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$Script:WorkspaceRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$Script:StateRoot = Join-Path $Script:WorkspaceRoot '.trae\tmp\dev-services'
$Script:LogRoot = Join-Path $Script:StateRoot 'logs'
$Script:StateFile = Join-Path $Script:StateRoot 'state.json'
$Script:DefaultPorts = @{
    HRMS = 5000
    ProcessCenter = 5099
    TodoCenter = 5163
    Frontend = 3000
}

function Ensure-DevDirectories {
    foreach ($path in @($Script:StateRoot, $Script:LogRoot)) {
        if (-not (Test-Path -LiteralPath $path)) {
            New-Item -ItemType Directory -Path $path | Out-Null
        }
    }
}

function ConvertTo-PowerShellLiteral {
    param([AllowNull()][string]$Value)

    if ($null -eq $Value) {
        return "''"
    }

    return "'" + $Value.Replace("'", "''") + "'"
}

function Get-DevState {
    if (-not (Test-Path -LiteralPath $Script:StateFile)) {
        return $null
    }

    return Get-Content -LiteralPath $Script:StateFile -Raw | ConvertFrom-Json
}

function Save-DevState {
    param(
        [hashtable]$Ports,
        [array]$Services
    )

    Ensure-DevDirectories

    $state = [pscustomobject]@{
        updatedAt = (Get-Date).ToString('s')
        ports = [pscustomobject]@{
            HRMS = $Ports.HRMS
            ProcessCenter = $Ports.ProcessCenter
            TodoCenter = $Ports.TodoCenter
            Frontend = $Ports.Frontend
        }
        services = @(
            foreach ($service in $Services) {
                [pscustomobject]@{
                    key = $service.Key
                    name = $service.Name
                    kind = $service.Kind
                    port = $service.Port
                    url = $service.Url
                    healthUrl = $service.HealthUrl
                    wrapperPid = $service.WrapperPid
                    stdout = $service.StdOutPath
                    stderr = $service.StdErrPath
                }
            }
        )
    }

    $state | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $Script:StateFile -Encoding utf8
}

function Remove-DevState {
    if (Test-Path -LiteralPath $Script:StateFile) {
        Remove-Item -LiteralPath $Script:StateFile -Force
    }
}

function Get-PortOwnerIds {
    param([int]$Port)

    $connections = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue
    if ($null -eq $connections) {
        return @()
    }

    return @($connections | Select-Object -ExpandProperty OwningProcess -Unique)
}

function Test-PortListening {
    param([int]$Port)

    return @((Get-PortOwnerIds -Port $Port)).Count -gt 0
}

function Stop-ProcessTree {
    param([int[]]$Ids)

    foreach ($id in ($Ids | Where-Object { $_ -gt 0 } | Select-Object -Unique)) {
        try {
            Stop-Process -Id $id -Force -ErrorAction Stop
        }
        catch {
        }
    }
}

function Invoke-ServiceProbe {
    param([pscustomobject]$Service)

    try {
        if ($Service.Kind -eq 'frontend') {
            $response = Invoke-WebRequest -Uri $Service.HealthUrl -UseBasicParsing -TimeoutSec 3
            return [pscustomobject]@{
                Healthy = ($response.StatusCode -ge 200 -and $response.StatusCode -lt 400)
                Detail = 'HTTP ' + $response.StatusCode
            }
        }

        $response = Invoke-RestMethod -Uri $Service.HealthUrl -TimeoutSec 3
        $healthy = $response.status -eq 'Healthy' -and $response.service -eq $Service.Name
        return [pscustomobject]@{
            Healthy = $healthy
            Detail = if ($healthy) { $response.service } else { 'Health payload mismatch' }
        }
    }
    catch {
        return [pscustomobject]@{
            Healthy = $false
            Detail = $_.Exception.Message
        }
    }
}

function Test-ServiceHealthy {
    param([pscustomobject]$Service)

    return (Invoke-ServiceProbe -Service $Service).Healthy
}

function Wait-ServiceHealthy {
    param(
        [pscustomobject]$Service,
        [int]$TimeoutSeconds
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        if (Test-ServiceHealthy -Service $Service) {
            return $true
        }

        Start-Sleep -Seconds 2
    }

    return $false
}

function Get-LogTail {
    param(
        [string]$Path,
        [int]$LineCount = 20
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        return ''
    }

    return ((Get-Content -LiteralPath $Path -Tail $LineCount -ErrorAction SilentlyContinue) -join [Environment]::NewLine)
}

function New-DetachedCommand {
    param(
        [string]$WorkingDirectory,
        [hashtable]$Environment,
        [string]$Executable,
        [string[]]$Arguments
    )

    $lines = @("Set-Location $(ConvertTo-PowerShellLiteral -Value $WorkingDirectory)")
    foreach ($key in ($Environment.Keys | Sort-Object)) {
        $value = [string]$Environment[$key]
        $lines += "`$env:$key = $(ConvertTo-PowerShellLiteral -Value $value)"
    }

    $quotedArguments = if ($Arguments.Count -gt 0) {
        ($Arguments | ForEach-Object { ConvertTo-PowerShellLiteral -Value ([string]$_) }) -join ', '
    }
    else {
        ''
    }

    $lines += "& $(ConvertTo-PowerShellLiteral -Value $Executable) @($quotedArguments)"
    return $lines -join '; '
}

function Start-ServiceProcess {
    param([pscustomobject]$Service)

    Ensure-DevDirectories

    $stdoutPath = Join-Path $Script:LogRoot ($Service.Key.ToLowerInvariant() + '.out.log')
    $stderrPath = Join-Path $Script:LogRoot ($Service.Key.ToLowerInvariant() + '.err.log')
    $command = New-DetachedCommand -WorkingDirectory $Service.WorkingDirectory -Environment $Service.Environment -Executable $Service.Executable -Arguments $Service.Arguments

    $process = Start-Process `
        -FilePath 'powershell.exe' `
        -ArgumentList @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-Command', $command) `
        -WorkingDirectory $Service.WorkingDirectory `
        -WindowStyle Hidden `
        -RedirectStandardOutput $stdoutPath `
        -RedirectStandardError $stderrPath `
        -PassThru

    $Service | Add-Member -NotePropertyName WrapperPid -NotePropertyValue $process.Id -Force
    $Service | Add-Member -NotePropertyName StdOutPath -NotePropertyValue $stdoutPath -Force
    $Service | Add-Member -NotePropertyName StdErrPath -NotePropertyValue $stderrPath -Force
}

function Get-PreferredPorts {
    $state = Get-DevState
    if ($null -eq $state -or $null -eq $state.ports) {
        return @{
            HRMS = $Script:DefaultPorts.HRMS
            ProcessCenter = $Script:DefaultPorts.ProcessCenter
            TodoCenter = $Script:DefaultPorts.TodoCenter
            Frontend = $Script:DefaultPorts.Frontend
        }
    }

    return @{
        HRMS = [int]$state.ports.HRMS
        ProcessCenter = [int]$state.ports.ProcessCenter
        TodoCenter = [int]$state.ports.TodoCenter
        Frontend = [int]$state.ports.Frontend
    }
}

function Find-AvailablePort {
    param(
        [string]$Key,
        [string]$Name,
        [string]$Kind,
        [int]$PreferredPort,
        [System.Collections.Generic.HashSet[int]]$ReservedPorts
    )

    for ($candidate = $PreferredPort; $candidate -lt ($PreferredPort + 50); $candidate++) {
        if ($ReservedPorts.Contains($candidate)) {
            continue
        }

        if (-not (Test-PortListening -Port $candidate)) {
            return $candidate
        }

        if ($Kind -eq 'backend') {
            $probeService = [pscustomobject]@{
                Key = $Key
                Name = $Name
                Kind = $Kind
                Port = $candidate
                HealthUrl = "http://127.0.0.1:$candidate/health"
            }

            if (Test-ServiceHealthy -Service $probeService) {
                return $candidate
            }
        }
    }

    throw "No available port found for $Name starting from $PreferredPort."
}

function Resolve-DevPorts {
    $preferred = Get-PreferredPorts
    $reserved = New-Object 'System.Collections.Generic.HashSet[int]'
    $ports = @{}

    $serviceDescriptors = @(
        @{ Key = 'HRMS'; Name = 'HRMS.API'; Kind = 'backend'; Preferred = $preferred.HRMS },
        @{ Key = 'ProcessCenter'; Name = 'ProcessCenter.API'; Kind = 'backend'; Preferred = $preferred.ProcessCenter },
        @{ Key = 'TodoCenter'; Name = 'TodoCenter.API'; Kind = 'backend'; Preferred = $preferred.TodoCenter },
        @{ Key = 'Frontend'; Name = 'hrms-web'; Kind = 'frontend'; Preferred = $preferred.Frontend }
    )

    foreach ($descriptor in $serviceDescriptors) {
        $port = Find-AvailablePort `
            -Key $descriptor.Key `
            -Name $descriptor.Name `
            -Kind $descriptor.Kind `
            -PreferredPort ([int]$descriptor.Preferred) `
            -ReservedPorts $reserved

        $ports[$descriptor.Key] = $port
        [void]$reserved.Add($port)
    }

    return $ports
}

function Get-DevServices {
    param([hashtable]$Ports)

    $hrmsUrl = "http://127.0.0.1:$($Ports.HRMS)"
    $processCenterUrl = "http://127.0.0.1:$($Ports.ProcessCenter)"
    $todoCenterUrl = "http://127.0.0.1:$($Ports.TodoCenter)"
    $frontendUrl = "http://127.0.0.1:$($Ports.Frontend)"
    $solutionRoot = $Script:WorkspaceRoot

    return @(
        [pscustomobject]@{
            Key = 'HRMS'
            Name = 'HRMS.API'
            Kind = 'backend'
            Port = $Ports.HRMS
            Url = $hrmsUrl
            HealthUrl = "$hrmsUrl/health"
            WorkingDirectory = $solutionRoot
            Executable = 'dotnet'
            Arguments = @('watch', '--project', (Join-Path $solutionRoot 'src\HRMS.API\HRMS.API.csproj'), 'run', '--no-launch-profile')
            Environment = @{
                ASPNETCORE_ENVIRONMENT = 'Development'
                ASPNETCORE_URLS = $hrmsUrl
                ProcessCenter__BaseUrl = $processCenterUrl
                TodoCenter__BaseUrl = $todoCenterUrl
            }
            StartTimeoutSeconds = 90
            WrapperPid = $null
            StdOutPath = $null
            StdErrPath = $null
        }
        [pscustomobject]@{
            Key = 'ProcessCenter'
            Name = 'ProcessCenter.API'
            Kind = 'backend'
            Port = $Ports.ProcessCenter
            Url = $processCenterUrl
            HealthUrl = "$processCenterUrl/health"
            WorkingDirectory = $solutionRoot
            Executable = 'dotnet'
            Arguments = @('watch', '--project', (Join-Path $solutionRoot 'src\ProcessCenter.API\ProcessCenter.API.csproj'), 'run', '--no-launch-profile')
            Environment = @{
                ASPNETCORE_ENVIRONMENT = 'Development'
                ASPNETCORE_URLS = $processCenterUrl
                TodoCenter__BaseUrl = $todoCenterUrl
                BusinessCallbacks__HrmsBaseUrl = $hrmsUrl
            }
            StartTimeoutSeconds = 90
            WrapperPid = $null
            StdOutPath = $null
            StdErrPath = $null
        }
        [pscustomobject]@{
            Key = 'TodoCenter'
            Name = 'TodoCenter.API'
            Kind = 'backend'
            Port = $Ports.TodoCenter
            Url = $todoCenterUrl
            HealthUrl = "$todoCenterUrl/health"
            WorkingDirectory = $solutionRoot
            Executable = 'dotnet'
            Arguments = @('watch', '--project', (Join-Path $solutionRoot 'src\TodoCenter.API\TodoCenter.API.csproj'), 'run', '--no-launch-profile')
            Environment = @{
                ASPNETCORE_ENVIRONMENT = 'Development'
                ASPNETCORE_URLS = $todoCenterUrl
                ProcessCenter__BaseUrl = $processCenterUrl
            }
            StartTimeoutSeconds = 90
            WrapperPid = $null
            StdOutPath = $null
            StdErrPath = $null
        }
        [pscustomobject]@{
            Key = 'Frontend'
            Name = 'hrms-web'
            Kind = 'frontend'
            Port = $Ports.Frontend
            Url = $frontendUrl
            HealthUrl = $frontendUrl
            WorkingDirectory = Join-Path $solutionRoot 'hrms-web'
            Executable = 'npm.cmd'
            Arguments = @('run', 'dev', '--', '--host', '127.0.0.1', '--port', [string]$Ports.Frontend)
            Environment = @{
                VITE_DEV_PORT = [string]$Ports.Frontend
                VITE_API_PROXY_TARGET = $hrmsUrl
            }
            StartTimeoutSeconds = 45
            WrapperPid = $null
            StdOutPath = $null
            StdErrPath = $null
        }
    )
}

function Stop-DevEnvironment {
    $state = Get-DevState
    $ports = Get-PreferredPorts
    $services = Get-DevServices -Ports $ports

    if ($null -ne $state -and $null -ne $state.services) {
        Stop-ProcessTree -Ids @($state.services | Where-Object { $_.wrapperPid } | ForEach-Object { [int]$_.wrapperPid })
    }

    foreach ($service in $services) {
        Stop-ProcessTree -Ids @(Get-PortOwnerIds -Port $service.Port)
    }

    Remove-DevState
}

function Start-DevEnvironment {
    param([switch]$ForceRestart)

    if ($ForceRestart) {
        Stop-DevEnvironment
    }

    $ports = Resolve-DevPorts
    $services = Get-DevServices -Ports $ports

    foreach ($service in ($services | Where-Object { $_.Kind -eq 'backend' })) {
        if (-not (Test-ServiceHealthy -Service $service)) {
            if (Test-PortListening -Port $service.Port) {
                Stop-ProcessTree -Ids @(Get-PortOwnerIds -Port $service.Port)
                Start-Sleep -Seconds 1
            }

            Start-ServiceProcess -Service $service
            if (-not (Wait-ServiceHealthy -Service $service -TimeoutSeconds $service.StartTimeoutSeconds)) {
                $outTail = Get-LogTail -Path $service.StdOutPath
                $errTail = Get-LogTail -Path $service.StdErrPath
                throw "Failed to start $($service.Name).`nSTDOUT:`n$outTail`nSTDERR:`n$errTail"
            }
        }
    }

    $frontend = $services | Where-Object { $_.Key -eq 'Frontend' } | Select-Object -First 1
    if (-not (Test-ServiceHealthy -Service $frontend)) {
        if (Test-PortListening -Port $frontend.Port) {
            Stop-ProcessTree -Ids @(Get-PortOwnerIds -Port $frontend.Port)
            Start-Sleep -Seconds 1
        }

        Start-ServiceProcess -Service $frontend
        if (-not (Wait-ServiceHealthy -Service $frontend -TimeoutSeconds $frontend.StartTimeoutSeconds)) {
            $outTail = Get-LogTail -Path $frontend.StdOutPath
            $errTail = Get-LogTail -Path $frontend.StdErrPath
            throw "Failed to start $($frontend.Name).`nSTDOUT:`n$outTail`nSTDERR:`n$errTail"
        }
    }

    Save-DevState -Ports $ports -Services $services
    return $services
}

function Get-DevEnvironmentStatus {
    $ports = Get-PreferredPorts
    $services = Get-DevServices -Ports $ports

    $rows = foreach ($service in $services) {
        $probe = Invoke-ServiceProbe -Service $service
        $listenerIds = @(Get-PortOwnerIds -Port $service.Port)
        [pscustomobject]@{
            Service = $service.Name
            Port = $service.Port
            Status = if ($probe.Healthy) { 'Running' } elseif ($listenerIds.Count -gt 0) { 'PortOccupied' } else { 'Stopped' }
            Url = $service.Url
            Health = $service.HealthUrl
            Detail = $probe.Detail
            Pids = if ($listenerIds.Count -gt 0) { ($listenerIds -join ',') } else { '-' }
        }
    }

    return $rows
}

function Show-DevEnvironmentStatus {
    $rows = Get-DevEnvironmentStatus
    $rows | Format-Table -AutoSize | Out-Host
    return $rows
}
