param([string]$GamePath='F:\Games\Steam\steamapps\common\Nuclear Option')
$ErrorActionPreference='Stop'
$managedPath=Join-Path $GamePath 'NuclearOption_Data\Managed'
$corePath=Join-Path $PSScriptRoot 'dependencies\BepInEx\BepInEx\core'
$compilerPath=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$outputPath=Join-Path $PSScriptRoot 'build'
New-Item -ItemType Directory -Path $outputPath -Force | Out-Null
$references=@(
 (Join-Path $managedPath 'mscorlib.dll'),(Join-Path $managedPath 'System.dll'),(Join-Path $managedPath 'System.Core.dll'),(Join-Path $managedPath 'netstandard.dll'),
 (Join-Path $corePath 'BepInEx.dll'),(Join-Path $corePath '0Harmony.dll'),
 (Join-Path $managedPath 'UnityEngine.dll'),(Join-Path $managedPath 'UnityEngine.CoreModule.dll'),
 (Join-Path $managedPath 'UnityEngine.InputLegacyModule.dll'),(Join-Path $managedPath 'UnityEngine.IMGUIModule.dll')
)
$compilerArgs=@('/nologo','/noconfig','/nostdlib+','/target:library','/platform:x64','/optimize+',('/out:'+(Join-Path $outputPath 'NuclearOption.CameraStepTest.dll')))
$compilerArgs+=@($references | ForEach-Object { '/reference:'+$_ })
$compilerArgs+=(Join-Path $PSScriptRoot 'CameraStepTest.cs')
& $compilerPath @compilerArgs
if($LASTEXITCODE -ne 0){throw 'Camera test compilation failed.'}
Write-Output 'Camera test plugin built.'
