# run-test-gha.ps1

# 1. Run dry-run validation on the full 72-plugins configuration
Write-Host "Running dry-run validation on all 72 plugins..."
./WinHome.exe --config test-config-full-plugins.yaml --dry-run --auto-install-apps --debug
$dryRunExitCode = $LASTEXITCODE

if ($dryRunExitCode -ne 0) {
Write-Error "WinHome.exe dry-run validation on all 72 plugins failed with exit code $dryRunExitCode"
exit $dryRunExitCode
}

# 2. Run live integration test on all 72 plugins
Write-Host "Running live integration test on all 72 plugins..."
./WinHome.exe --config test-config-full-plugins.yaml --auto-install-apps --debug
$winhomeExitCode = $LASTEXITCODE

if ($winhomeExitCode -ne 0) {
Write-Error "WinHome.exe live test on all 72 plugins failed with exit code $winhomeExitCode"
exit $winhomeExitCode
}

# 3. Run verification script for the live test assertions
./verify-gha.ps1
$verifyExitCode = $LASTEXITCODE

exit $verifyExitCode
