$ErrorActionPreference = 'Stop'
$compilerPath = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compilerPath)) { throw 'Windows .NET Framework 4.x C# compiler was not found.' }
$projectPath = $PSScriptRoot
$binaryPath = Join-Path $projectPath 'SteamControllerGyroToUDP.exe'
& $compilerPath /nologo /target:winexe /platform:x64 /optimize+ "/out:$binaryPath" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Xml.dll (Join-Path $projectPath 'NativeHid.cs') (Join-Path $projectPath 'Motion.cs') (Join-Path $projectPath 'Program.cs') (Join-Path $projectPath 'Hotkeys.cs') (Join-Path $projectPath 'Tests.cs')
if ($LASTEXITCODE -ne 0) { throw 'Compilation failed.' }
$testProcess = Start-Process -FilePath $binaryPath -ArgumentList '--self-test' -Wait -PassThru -WindowStyle Hidden
Get-Content -LiteralPath (Join-Path $projectPath 'self-test.txt')
if ($testProcess.ExitCode -ne 0) { throw 'Self-tests failed.' }
