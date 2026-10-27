$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$workspaceRoot = Split-Path $projectRoot -Parent
$release = Get-Content -LiteralPath (Join-Path $workspaceRoot '.tools\unity-release.json') -Raw | ConvertFrom-Json
$windows = $release.results[0].downloads | Where-Object { $_.platform -eq 'WINDOWS' -and $_.architecture -eq 'X86_64' }
$module = $windows.modules | Where-Object id -EQ 'android'
$installer = Join-Path (Join-Path $workspaceRoot '.tools\downloads') ($module.url.Split('/')[-1])
$editorRoot = Join-Path $workspaceRoot '.tools\Unity\6000.3.27f1'
if (-not (Test-Path -LiteralPath (Join-Path $editorRoot 'Editor\Unity.exe'))) { throw 'Install the Editor first.' }
if (-not (Test-Path -LiteralPath $installer)) { throw 'Android support download is incomplete.' }
if ((Get-Item -LiteralPath $installer).Length -ne [long]$module.downloadSize.value) { throw 'Android support download is incomplete.' }
$algorithm, $digest = $module.integrity.Split('-', 2)
$expectedHash = ([BitConverter]::ToString([Convert]::FromBase64String($digest))).Replace('-', '').ToLowerInvariant()
if ((Get-FileHash -LiteralPath $installer -Algorithm $algorithm).Hash.ToLowerInvariant() -ne $expectedHash) { throw 'Android installer differs from the official HTTPS Unity release metadata.' }
if (-not (Test-Path -LiteralPath (Join-Path $editorRoot 'Editor\Data\PlaybackEngines\AndroidPlayer\UnityEditor.Android.Extensions.dll')))
{
    $process = Start-Process -FilePath $installer -ArgumentList "/S /D=$editorRoot" -WindowStyle Hidden -Wait -PassThru
    if ($process.ExitCode -ne 0) { throw "Android support installer failed: $($process.ExitCode)" }
}
& python (Join-Path $PSScriptRoot 'install_android_zips.py')
if ($LASTEXITCODE -ne 0) { throw 'Android ZIP module installation failed.' }
Write-Output 'Android editor support and official ZIP modules are installed.'
