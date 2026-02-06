# start-sandbox.ps1
$ErrorActionPreference = "Stop"

Write-Host "--- WinHome Sandbox Launcher ---" -ForegroundColor Cyan

# 1. Build the project
Write-Host "Building WinHome (Release, Self-Contained)..." -ForegroundColor Yellow
dotnet publish ../../src/WinHome.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ../../publish /v:q /nologo

if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed. Aborting sandbox launch."
    exit 1
}

Write-Host "Build successful." -ForegroundColor Green

# 2. Launch Sandbox
$sandboxFile = Join-Path $PSScriptRoot "WinHome.wsb"

if (-not (Test-Path $sandboxFile)) {
    Write-Error "WinHome.wsb not found at $sandboxFile"
    exit 1
}

Write-Host "Launching Windows Sandbox..." -ForegroundColor Yellow
Invoke-Item $sandboxFile

Write-Host "Sandbox launched. Changes inside the sandbox are lost upon closing." -ForegroundColor Gray
