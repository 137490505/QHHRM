. (Join-Path $PSScriptRoot 'dev-common.ps1')

$status = Show-DevEnvironmentStatus

Write-Host ''
Write-Host 'Available URLs:'
foreach ($row in $status) {
    Write-Host ("- {0}: {1}" -f $row.Service, $row.Url)
}
