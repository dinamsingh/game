$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$workspaceRoot = Split-Path $projectRoot -Parent
$installer = Join-Path $workspaceRoot '.tools\downloads\UnitySetup64-6000.3.27f1.exe'
$editorRoot = Join-Path $workspaceRoot '.tools\Unity\6000.3.27f1'
$release = Get-Content -LiteralPath (Join-Path $workspaceRoot '.tools\unity-release.json') -Raw | ConvertFrom-Json
$expected = $release.results[0].downloads | Where-Object { $_.platform -eq 'WINDOWS' -and $_.architecture -eq 'X86_64' }
if (-not (Test-Path -LiteralPath $installer)) { throw 'Official editor download is not complete yet.' }
if ((Get-Item -LiteralPath $installer).Length -ne [long]$expected.downloadSize.value) { throw 'Editor download is incomplete.' }
$algorithm, $digest = $expected.integrity.Split('-', 2)
$expectedHash = ([BitConverter]::ToString([Convert]::FromBase64String($digest))).Replace('-', '').ToLowerInvariant()
$actualHash = (Get-FileHash -LiteralPath $installer -Algorithm $algorithm).Hash.ToLowerInvariant()
if ($actualHash -ne $expectedHash) { throw 'Installer checksum differs from the official HTTPS Unity release metadata.' }
Write-Output 'Official Unity release checksum verified.'
New-Item -ItemType Directory -Path $editorRoot -Force | Out-Null
if (-not (Test-Path -LiteralPath (Join-Path $editorRoot 'Editor\Unity.exe')))
{
    $process = Start-Process -FilePath $installer -ArgumentList "/S /D=$editorRoot" -WindowStyle Hidden -Wait -PassThru
    if ($process.ExitCode -ne 0) { throw "Unity installer returned $($process.ExitCode)." }
}
if (-not (Test-Path -LiteralPath (Join-Path $editorRoot 'Editor\Unity.exe'))) { throw 'Editor executable was not created.' }
Write-Output "Unity Editor ready: $editorRoot"
