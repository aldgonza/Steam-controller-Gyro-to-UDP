$ErrorActionPreference='Stop'
& (Join-Path $PSScriptRoot 'build.ps1')
& (Join-Path $PSScriptRoot 'validate.ps1')
$packageRoot=Join-Path $PSScriptRoot ('build\package-'+[Guid]::NewGuid().ToString('N'))
$pluginPath=Join-Path $packageRoot 'game-files\BepInEx\plugins\NuclearOption.CameraStepTest'
$distPath=Join-Path $PSScriptRoot 'dist'
New-Item -ItemType Directory -Path $pluginPath,$distPath -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'build\NuclearOption.CameraStepTest.dll') -Destination $pluginPath
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'README.md') -Destination $packageRoot
$hash=(Get-FileHash -LiteralPath (Join-Path $pluginPath 'NuclearOption.CameraStepTest.dll') -Algorithm SHA256).Hash.ToLowerInvariant()
Set-Content -LiteralPath (Join-Path $packageRoot 'SHA256SUMS.txt') -Value "$hash  game-files/BepInEx/plugins/NuclearOption.CameraStepTest/NuclearOption.CameraStepTest.dll" -Encoding ascii
$zipPath=Join-Path $distPath 'NuclearOption-CameraStepTest-0.1.0.zip'
Compress-Archive -LiteralPath (Join-Path $packageRoot 'game-files'),(Join-Path $packageRoot 'README.md'),(Join-Path $packageRoot 'SHA256SUMS.txt') -DestinationPath $zipPath -Force
Write-Output "Built $zipPath"
