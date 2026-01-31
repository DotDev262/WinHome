# run-test-container.ps1

# Run WinHome
./WinHome.exe --config test-config-container.yaml --debug
$winhomeExitCode = $LASTEXITCODE

if ($winhomeExitCode -ne 0) {
    Write-Error "WinHome.exe failed with exit code $winhomeExitCode"
    exit $winhomeExitCode
}

# Run verification script
./verify-container.ps1
$verifyExitCode = $LASTEXITCODE

exit $verifyExitCode
