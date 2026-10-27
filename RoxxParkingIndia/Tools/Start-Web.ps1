$ErrorActionPreference='Stop'
$projectRoot=Split-Path $PSScriptRoot -Parent
$node=(Get-Command node -ErrorAction Stop).Source
$address=Get-NetIPAddress -InterfaceAlias 'Wi-Fi' -AddressFamily IPv4 | Where-Object { $_.AddressState -eq 'Preferred' -and $_.IPAddress -notlike '169.254.*' } | Select-Object -First 1 -ExpandProperty IPAddress
if(-not $address){throw 'Connect the laptop to Wi-Fi first.'}
$url="http://${address}:8877/"
$listening=Get-NetTCPConnection -LocalPort 8877 -State Listen -ErrorAction SilentlyContinue
if($listening){
    $owner=Get-CimInstance Win32_Process -Filter "ProcessId=$($listening[0].OwningProcess)"
    $server=Join-Path $PSScriptRoot 'serve-web.cjs'
    if(-not $owner.CommandLine.Contains($server)){throw 'Port 8877 is used by another application; it has been left running.'}
    if($listening[0].LocalAddress -ne $address){throw 'Wi-Fi address changed. Close the old game server before starting it again.'}
}else{
    New-Item -ItemType Directory -Path (Join-Path $projectRoot 'Logs') -Force | Out-Null
    $process=Start-Process -FilePath $node -ArgumentList @(('"'+(Join-Path $PSScriptRoot 'serve-web.cjs')+'"'),$address) -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $projectRoot 'Logs/Web-server.log') -RedirectStandardError (Join-Path $projectRoot 'Logs/Web-server-error.log')
    $process.Id | Set-Content (Join-Path $projectRoot 'Logs/Web-server.pid')
}
for($attempt=0;$attempt -lt 20;$attempt++){
    try{$health=Invoke-RestMethod ($url+'health') -TimeoutSec 2;if($health.game -eq 'Roxx Parking India'){break}}catch{Start-Sleep -Milliseconds 500}
}
if(-not $health -or $health.game -ne 'Roxx Parking India'){throw 'Game server failed to start. See Logs/Web-server-error.log.'}
Write-Output "Open this link in Chrome on your Android: $url"
Write-Output 'Keep the laptop on and connected to the same Wi-Fi.'
$url | Set-Content (Join-Path $projectRoot 'Logs/Phone-link.txt')
