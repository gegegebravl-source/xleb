param(
    [Parameter(Mandatory = $true)]
    [string]$UnityPath,
    [string]$ProjectPath = (Resolve-Path "$PSScriptRoot\.."),
    [string]$Results = "$PSScriptRoot\..\TestResults\editmode.xml",
    [string]$LogFile = "$PSScriptRoot\..\Logs\test-run.log"
)

$ErrorActionPreference = "Stop"
New-Item -ItemType Directory -Force (Split-Path $Results) | Out-Null
New-Item -ItemType Directory -Force (Split-Path $LogFile) | Out-Null
& $UnityPath -batchmode -nographics -quit `
    -projectPath $ProjectPath `
    -runTests -testPlatform EditMode `
    -testResults $Results `
    -logFile $LogFile

if ($LASTEXITCODE -ne 0) {
    throw "Unity tests failed with exit code $LASTEXITCODE. See $LogFile"
}

Write-Host "EditMode tests passed. Results: $Results"
