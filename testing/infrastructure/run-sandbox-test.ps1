# run-sandbox-test.ps1
$ErrorActionPreference = "Stop"

# 1. Run Setup
Write-Host "`n--- [STAGE 1] Setting up Sandbox Environment ---" -ForegroundColor Cyan
powershell -ExecutionPolicy Bypass -File ./setup-sandbox.ps1

# 2. Run Test
Write-Host "`n--- [STAGE 2] Running WinHome Test Configuration ---" -ForegroundColor Cyan
../../publish/WinHome.exe --config ../../test-data/test-config.yaml

# 3. Run Verification
Write-Host "`n--- [STAGE 3] Running Automated Verification ---" -ForegroundColor Cyan
cd ../../test-data
Invoke-Pester ./verify.Tests.ps1

Write-Host "`nAll Sandbox tests completed!" -ForegroundColor Green
