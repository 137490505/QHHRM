. (Join-Path $PSScriptRoot 'dev-common.ps1')

Stop-DevEnvironment
$status = Show-DevEnvironmentStatus

Write-Host ''
Write-Host 'Development environment stopped.'
