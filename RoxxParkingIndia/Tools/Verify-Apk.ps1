$ErrorActionPreference='Stop'
$projectRoot=Split-Path $PSScriptRoot -Parent
$androidRoot=Join-Path (Split-Path $projectRoot -Parent) '.tools\Unity\6000.3.27f1\Editor\Data\PlaybackEngines\AndroidPlayer'
$buildTools=Join-Path $androidRoot 'SDK\build-tools\36.0.0'
$java=Join-Path $androidRoot 'OpenJDK\bin\java.exe'
$signer=Join-Path $buildTools 'lib\apksigner.jar'
$apk=Join-Path $projectRoot 'Builds\Android\RoxxParkingIndia.apk'
$signature=@(& $java -jar $signer verify --verbose --print-certs $apk)
if($LASTEXITCODE -ne 0){throw 'APK signature verification failed'}
$metadata=@(& (Join-Path $buildTools 'aapt.exe') dump badging $apk)
if($LASTEXITCODE -ne 0){throw 'APK metadata inspection failed'}
if($metadata[0] -notmatch "name='com.indie.roxxparkingindia' versionCode='7' versionName='0.6.1'"){throw 'Unexpected APK package or version'}
if(-not ($metadata -match "^sdkVersion:'26'")){throw 'Unexpected Android minimum API'}
if(-not ($metadata -match "^native-code: 'arm64-v8a'")){throw 'ARM64 native libraries are missing'}
if(-not ($metadata -match '^launchable-activity:')){throw 'APK has no launchable game activity'}
$certificate=($signature | Where-Object {$_ -match 'certificate SHA-256 digest:'} | Select-Object -First 1) -replace '^.*digest: ',''
$previous=Join-Path $projectRoot 'Builds\Android\previous\RoxxParkingIndia-0.5.0.apk'
$oldSignature=@(& $java -jar $signer verify --print-certs $previous)
if($LASTEXITCODE -ne 0){throw 'Previous APK signature verification failed'}
$oldCertificate=($oldSignature | Where-Object {$_ -match 'certificate SHA-256 digest:'} | Select-Object -First 1) -replace '^.*digest: ',''
if($certificate -ne $oldCertificate){throw 'Signing certificate differs: in-place update would fail'}
$report=[ordered]@{
    version='0.6.1';versionCode=7;bytes=(Get-Item -LiteralPath $apk).Length
    sha256=(Get-FileHash -LiteralPath $apk -Algorithm SHA256).Hash
    signatureVerified=$true;sameCertificateAsPrevious=$true;certificateSha256=$certificate
    metadata=@($metadata | Where-Object {$_ -match '^package:|^sdkVersion:|^targetSdkVersion:|^native-code:|^application-label:|^launchable-activity:'})
}
$report | ConvertTo-Json -Depth 3 | Set-Content -LiteralPath (Join-Path $projectRoot 'Builds\Checks\apk-verification.json') -Encoding UTF8
$report | ConvertTo-Json -Depth 3
