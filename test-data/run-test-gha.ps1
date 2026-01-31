# run-test-gha.ps1

# Run WinHome
./WinHome.exe --config test-config-gha.yaml --debug
$winhomeExitCode = $LASTEXITCODE

if ($winhomeExitCode -ne 0) {
    Write-Error "WinHome.exe failed with exit code $winhomeExitCode"
    exit $winhomeExitCode
}

# Run verification script
./verify-gha.ps1
$verifyExitCode = $LASTEXITCODE

exit $verifyExitCode
