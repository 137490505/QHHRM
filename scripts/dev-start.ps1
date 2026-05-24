. (Join-Path $PSScriptRoot 'dev-common.ps1')

$services = Start-DevEnvironment
$status = Show-DevEnvironmentStatus

Write-Host ''
Write-Host 'Development environment started:'
foreach ($row in $status) {
    Write-Host ("- {0}: {1}" -f $row.Service, $row.Url)
}
