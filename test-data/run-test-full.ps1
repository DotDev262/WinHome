# run-test-full.ps1

# Run WinHome
./WinHome.exe --config test-config-full.yaml --debug
$winhomeExitCode = $LASTEXITCODE

if ($winhomeExitCode -ne 0) {
    Write-Error "WinHome.exe failed with exit code $winhomeExitCode"
    exit $winhomeExitCode
}

# Run verification script
./verify-full.ps1
$verifyExitCode = $LASTEXITCODE

exit $verifyExitCode
