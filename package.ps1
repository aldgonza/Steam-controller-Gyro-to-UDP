$ErrorActionPreference = 'Stop'
$projectPath = $PSScriptRoot
$version = (Get-Content -LiteralPath (Join-Path $projectPath 'VERSION') -Raw).Trim()
if ($version -notmatch '^\d+\.\d+\.\d+(?:-[A-Za-z0-9.-]+)?$') { throw 'VERSION must contain a semantic version.' }
& (Join-Path $projectPath 'build.ps1')

$stageRoot = Join-Path $projectPath ('build\package-' + [Guid]::NewGuid().ToString('N'))
$packageName = "Steam-controller-Gyro-to-UDP-$version"
$stagePath = Join-Path $stageRoot $packageName
$distPath = Join-Path $projectPath 'dist'
New-Item -ItemType Directory -Path $stagePath, $distPath -Force | Out-Null
$packageFiles = @('SteamControllerGyroToUDP.exe','NativeHid.cs','Motion.cs','Program.cs','Keybinds.cs','Tests.cs','build.ps1','package.ps1','VERSION','README.md','VALIDATION.md','LICENSE.txt','THIRD-PARTY-NOTICES.txt','SECURITY.md','CONTRIBUTING.md','RELEASE_NOTES.md')
foreach ($packageFile in $packageFiles) {
    Copy-Item -LiteralPath (Join-Path $projectPath $packageFile) -Destination $stagePath
}
$exeHash = (Get-FileHash -LiteralPath (Join-Path $stagePath 'SteamControllerGyroToUDP.exe') -Algorithm SHA256).Hash.ToLowerInvariant()
Set-Content -LiteralPath (Join-Path $stagePath 'EXE-SHA256.txt') -Value "$exeHash  SteamControllerGyroToUDP.exe" -Encoding ascii
$zipPath = Join-Path $distPath "$packageName-windows-x64.zip"
Compress-Archive -LiteralPath $stagePath -DestinationPath $zipPath -Force
$zipHash = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()
Set-Content -LiteralPath (Join-Path $distPath 'SHA256SUMS.txt') -Value "$zipHash  $packageName-windows-x64.zip" -Encoding ascii
Write-Output "Built $zipPath"
