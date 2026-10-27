$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$toolRoot = Join-Path (Split-Path $projectRoot -Parent) '.tools'
$compilerRoot = Join-Path $toolRoot 'roslyn\tasks\netcore\bincore'
$compiler = Join-Path $compilerRoot 'csc.dll'
if (-not (Test-Path -LiteralPath $compiler)) { throw 'The local Microsoft compiler package has not been extracted.' }
$runtimeRoot = Get-ChildItem -LiteralPath 'C:\Program Files\dotnet\shared\Microsoft.NETCore.App' -Directory |
    Where-Object { $_.Name -like '8.*' } | Sort-Object { [version]$_.Name } -Descending | Select-Object -First 1
if (-not $runtimeRoot) { throw '.NET 8 runtime is required for these checks.' }
$outputRoot = Join-Path $projectRoot 'Tests\bin'
New-Item -ItemType Directory -Path $outputRoot -Force | Out-Null
$referenceNames = @('System.Private.CoreLib.dll','System.Runtime.dll','System.Console.dll','System.Collections.dll','System.Collections.Immutable.dll','System.Linq.dll','System.Reflection.Metadata.dll')
$references = @($referenceNames | ForEach-Object { '-r:"' + (Join-Path $runtimeRoot.FullName $_) + '"' })
$utf8 = New-Object System.Text.UTF8Encoding($false)
$runtimeConfig = '{"runtimeOptions":{"tfm":"net8.0","framework":{"name":"Microsoft.NETCore.App","version":"8.0.0"},"rollForward":"LatestPatch"}}'

$rulesOutput = Join-Path $outputRoot 'ParkingRulesSelfCheck.dll'
$rulesArgs = @('-nologo', '-nostdlib+', '-target:exe', ('-out:"' + $rulesOutput + '"')) + $references + @(
    ('"' + (Join-Path $projectRoot 'Assets\Scripts\ParkingRules.cs') + '"'),
    ('"' + (Join-Path $projectRoot 'Tests\ParkingRulesSelfCheck.cs') + '"')
)
$response = Join-Path $outputRoot 'rules.rsp'
[IO.File]::WriteAllLines($response, [string[]]$rulesArgs, $utf8)
& dotnet $compiler ('@' + $response)
if ($LASTEXITCODE -ne 0) { throw 'Parking rule compilation failed.' }
[IO.File]::WriteAllText((Join-Path $outputRoot 'ParkingRulesSelfCheck.runtimeconfig.json'), $runtimeConfig, $utf8)
& dotnet $rulesOutput
if ($LASTEXITCODE -ne 0) { throw 'Parking rule checks failed.' }

$syntaxOutput = Join-Path $outputRoot 'SyntaxCheck.dll'
$syntaxArgs = @('-nologo', '-nostdlib+', '-target:exe', ('-out:"' + $syntaxOutput + '"')) + $references + @(
    ('-r:"' + (Join-Path $compilerRoot 'Microsoft.CodeAnalysis.dll') + '"'),
    ('-r:"' + (Join-Path $compilerRoot 'Microsoft.CodeAnalysis.CSharp.dll') + '"'),
    ('"' + (Join-Path $projectRoot 'Tests\SyntaxCheck.cs') + '"')
)
$syntaxResponse = Join-Path $outputRoot 'syntax.rsp'
[IO.File]::WriteAllLines($syntaxResponse, [string[]]$syntaxArgs, $utf8)
& dotnet $compiler ('@' + $syntaxResponse)
if ($LASTEXITCODE -ne 0) { throw 'Syntax checker compilation failed.' }
Copy-Item -LiteralPath (Join-Path $compilerRoot 'Microsoft.CodeAnalysis.dll'),(Join-Path $compilerRoot 'Microsoft.CodeAnalysis.CSharp.dll') -Destination $outputRoot -Force
[IO.File]::WriteAllText((Join-Path $outputRoot 'SyntaxCheck.runtimeconfig.json'), $runtimeConfig, $utf8)
& dotnet $syntaxOutput (Join-Path $projectRoot 'Assets')
if ($LASTEXITCODE -ne 0) { throw 'Game script syntax checks failed.' }
& python (Join-Path $PSScriptRoot 'check_courses.py')
if ($LASTEXITCODE -ne 0) { throw 'Course validation failed.' }
