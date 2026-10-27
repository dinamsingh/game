$ErrorActionPreference='Stop'
$workspaceRoot=Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$release=Get-Content -LiteralPath (Join-Path $workspaceRoot '.tools\unity-release.json') -Raw | ConvertFrom-Json
$windows=$release.results[0].downloads | Where-Object { $_.platform -eq 'WINDOWS' -and $_.architecture -eq 'X86_64' }
$module=$windows.modules | Where-Object id -eq 'webgl'
$installer=Join-Path $workspaceRoot ('.tools\downloads\'+$module.url.Split('/')[-1])
$editorRoot=Join-Path $workspaceRoot '.tools\Unity\6000.3.27f1'
if((Get-Item -LiteralPath $installer).Length -ne [long]$module.downloadSize.value){throw 'Web module download incomplete'}
$algorithm,$digest=$module.integrity.Split('-',2)
$expected=([BitConverter]::ToString([Convert]::FromBase64String($digest))).Replace('-','')
if((Get-FileHash -LiteralPath $installer -Algorithm $algorithm).Hash -ne $expected){throw 'Web module integrity mismatch'}
$extension=Join-Path $editorRoot 'Editor\Data\PlaybackEngines\WebGLSupport\UnityEditor.WebGL.Extensions.dll'
if(-not (Test-Path -LiteralPath $extension))
{
    $process=Start-Process -FilePath $installer -ArgumentList "/S /D=$editorRoot" -WindowStyle Hidden -Wait -PassThru
    if($process.ExitCode -ne 0){throw "Web module installer failed: $($process.ExitCode)"}
}
if(-not (Test-Path -LiteralPath $extension)){throw 'Web module extension was not installed'}
'Official Web Build Support installed in project-local Unity Editor.'
