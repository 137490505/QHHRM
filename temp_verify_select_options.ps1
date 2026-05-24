$ErrorActionPreference = 'Stop'
$base = 'http://localhost:5000/api/v1'
$loginBody = @{ username = 'admin'; password = '123456' } | ConvertTo-Json
$login = Invoke-RestMethod -Uri "$base/auth/login" -Method Post -ContentType 'application/json' -Body $loginBody
$headers = @{ Authorization = "Bearer $($login.data.token)" }
$category = 'employeeTag'
$tempValue = 'temp_option_check_2'
$tempLabel1 = 'TempCheck2'
$tempLabel2 = 'TempCheck2Edited'

$original = @((Invoke-RestMethod -Uri "$base/settings/select-options/$category" -Headers $headers -Method Get).data)
$withTemp = @($original) + @([pscustomobject]@{ value = $tempValue; label = $tempLabel1; labelEn = $null })
$body = @{ options = @($withTemp) } | ConvertTo-Json -Depth 8
Invoke-RestMethod -Uri "$base/settings/select-options/$category" -Headers $headers -Method Put -ContentType 'application/json' -Body $body | Out-Null

$afterAdd = @((Invoke-RestMethod -Uri "$base/settings/select-options/$category" -Headers $headers -Method Get).data)
if (-not ($afterAdd | Where-Object { $_.value -eq $tempValue -and $_.label -eq $tempLabel1 })) { throw 'add_failed' }

$edited = foreach ($item in $afterAdd) {
  if ($item.value -eq $tempValue) {
    [pscustomobject]@{ value = $item.value; label = $tempLabel2; labelEn = $item.labelEn }
  }
  else {
    [pscustomobject]@{ value = $item.value; label = $item.label; labelEn = $item.labelEn }
  }
}
$body = @{ options = @($edited) } | ConvertTo-Json -Depth 8
Invoke-RestMethod -Uri "$base/settings/select-options/$category" -Headers $headers -Method Put -ContentType 'application/json' -Body $body | Out-Null

$afterEdit = @((Invoke-RestMethod -Uri "$base/settings/select-options/$category" -Headers $headers -Method Get).data)
if (-not ($afterEdit | Where-Object { $_.value -eq $tempValue -and $_.label -eq $tempLabel2 })) { throw 'edit_failed' }

$afterDeleteList = @($afterEdit | Where-Object { $_.value -ne $tempValue })
$body = @{ options = @($afterDeleteList) } | ConvertTo-Json -Depth 8
Invoke-RestMethod -Uri "$base/settings/select-options/$category" -Headers $headers -Method Put -ContentType 'application/json' -Body $body | Out-Null

$afterDelete = @((Invoke-RestMethod -Uri "$base/settings/select-options/$category" -Headers $headers -Method Get).data)
if ($afterDelete | Where-Object { $_.value -eq $tempValue }) { throw 'delete_failed' }

$body = @{ options = @($original) } | ConvertTo-Json -Depth 8
Invoke-RestMethod -Uri "$base/settings/select-options/$category" -Headers $headers -Method Put -ContentType 'application/json' -Body $body | Out-Null
Write-Output 'verification_passed'
