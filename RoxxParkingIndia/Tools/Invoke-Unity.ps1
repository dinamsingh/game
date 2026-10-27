param([ValidateSet('Open','Prepare','Check','Windows','Android','Web','Model')][string]$Action = 'Open')
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$workspaceRoot = Split-Path $projectRoot -Parent
$editorRoot = Join-Path $workspaceRoot '.tools\Unity\6000.3.27f1'
$editor = Join-Path $editorRoot 'Editor\Unity.exe'
if (-not (Test-Path -LiteralPath $editor)) { throw "Unity Editor is not installed yet: $editor" }
if ($Action -eq 'Open')
{
    Start-Process -FilePath $editor -WorkingDirectory $projectRoot -ArgumentList @('-projectPath', ('"' + $projectRoot + '"'), '-force-d3d11', '-job-worker-count', '2')
    exit 0
}
$logRoot = Join-Path $projectRoot 'Logs'
New-Item -ItemType Directory -Path $logRoot -Force | Out-Null
$method = switch ($Action) { 'Prepare' { 'Prepare' } 'Check' { 'SmokeTest' } 'Windows' { 'Windows' } 'Android' { 'Android' } 'Web' {'Web'} }
$entryPoint = if ($Action -eq 'Model') { 'TharModelBuilder.Prepare' } else { 'PrototypeBuild.' + $method }
$arguments = @('-batchmode', '-projectPath', ('"' + $projectRoot + '"'), '-executeMethod', $entryPoint,
    '-logFile', ('"' + (Join-Path $logRoot ($Action + '.log')) + '"'), '-job-worker-count', '2')
if ($Action -eq 'Android') { $arguments += @('-buildTarget', 'Android') }
elseif ($Action -eq 'Web') { $arguments += @('-buildTarget', 'WebGL') }
elseif ($Action -eq 'Windows') { $arguments += @('-buildTarget', 'StandaloneWindows64') }
if ($Action -eq 'Check') { $arguments += @('-force-d3d11','--roxx-smoke') } else { $arguments += @('-nographics','-quit') }
$process = Start-Process -FilePath $editor -WorkingDirectory $projectRoot -WindowStyle Hidden -ArgumentList $arguments -PassThru -Wait
if ($process.ExitCode -ne 0) { throw "Unity $Action failed with exit code $($process.ExitCode). See Logs\$Action.log" }
Write-Output "Unity $Action finished successfully."
