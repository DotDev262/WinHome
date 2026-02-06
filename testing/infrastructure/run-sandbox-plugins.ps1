# run-sandbox-plugins.ps1
$ErrorActionPreference = "Stop"

# 1. Run Setup (Copies plugins, upgrades Pester, installs runtimes)
Write-Host "`n--- [STAGE 1] Setting up Sandbox Environment ---" -ForegroundColor Cyan
powershell -ExecutionPolicy Bypass -File ./setup-sandbox.ps1

# 2. Run Plugin-Only Configuration
Write-Host "`n--- [STAGE 2] Running WinHome Plugins-Only Configuration ---" -ForegroundColor Cyan
../../publish/WinHome.exe --config ../../test-data/test-plugins-only.yaml

# 3. Verify
Write-Host "`n--- [STAGE 3] Verification ---" -ForegroundColor Cyan
Write-Host "Checking Vim Config..." -ForegroundColor Gray
$nvimPath = Join-Path $env:LOCALAPPDATA "nvim\init.lua"
if (Test-Path $nvimPath) { Write-Host "[OK] init.lua found" -ForegroundColor Green }
Write-Host "Checking VSCode Config..." -ForegroundColor Gray
$vscSettings = Join-Path $env:APPDATA "Code\User\settings.json"
if (Test-Path $vscSettings) { Write-Host "[OK] settings.json found" -ForegroundColor Green }

Write-Host "`nPlugin-only tests completed!" -ForegroundColor Green
