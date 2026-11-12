$ErrorActionPreference='Stop'
$dependenciesPath=Join-Path $PSScriptRoot 'dependencies'
New-Item -ItemType Directory -Path $dependenciesPath -Force | Out-Null
$archivePath=Join-Path $dependenciesPath 'BepInEx_win_x64_5.4.23.6.zip'
Invoke-WebRequest -Uri 'https://github.com/BepInEx/BepInEx/releases/download/v5.4.23.6/BepInEx_win_x64_5.4.23.6.zip' -OutFile $archivePath
$expectedHash='6c57a714a892efb4e9e1fef63d608a3391f30ec984031c465404e4ee381a0755'
if((Get-FileHash -LiteralPath $archivePath -Algorithm SHA256).Hash.ToLowerInvariant() -ne $expectedHash){throw 'Downloaded loader differs from the release used for this experiment. Review before proceeding.'}
Expand-Archive -LiteralPath $archivePath -DestinationPath (Join-Path $dependenciesPath 'BepInEx') -Force
Write-Output 'BepInEx dependencies prepared. No game installation changes made.'
