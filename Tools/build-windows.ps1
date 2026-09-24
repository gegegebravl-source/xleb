param(
    [Parameter(Mandatory = $true)]
    [string]$UnityPath,
    [string]$ProjectPath = (Resolve-Path "$PSScriptRoot\.."),
    [string]$LogFile = "$PSScriptRoot\..\Logs\build-windows.log"
)

$ErrorActionPreference = "Stop"
New-Item -ItemType Directory -Force (Split-Path $LogFile) | Out-Null
& $UnityPath -batchmode -nographics -quit `
    -projectPath $ProjectPath `
    -executeMethod WarmBread.Editor.ReleaseBuilder.BuildWindows `
    -logFile $LogFile

if ($LASTEXITCODE -ne 0) {
    throw "Unity build failed with exit code $LASTEXITCODE. See $LogFile"
}

Write-Host "Release build created in Builds/Windows"
