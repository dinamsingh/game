$ErrorActionPreference='Stop'
$pidFile=Join-Path (Split-Path $PSScriptRoot -Parent) 'Logs/Web-server.pid'
if(-not (Test-Path -LiteralPath $pidFile)){'Game server is already stopped.';exit}
$serverPid=[int](Get-Content -LiteralPath $pidFile)
$process=Get-CimInstance Win32_Process -Filter "ProcessId=$serverPid"
$server=Join-Path $PSScriptRoot 'serve-web.cjs'
if($process){
    if($process.Name -ne 'node.exe' -or -not $process.CommandLine -or -not $process.CommandLine.Contains($server)){throw 'Recorded process does not match the game server; it has been left running.'}
    Stop-Process -Id $serverPid -ErrorAction Stop
}
Remove-Item -LiteralPath $pidFile
'Game server stopped.'
