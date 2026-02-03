# run-test.ps1

# Run WinHome
./WinHome.exe --config test-config.yaml --debug
$winhomeExitCode = $LASTEXITCODE

if ($winhomeExitCode -ne 0) {
    Write-Error "WinHome.exe failed with exit code $winhomeExitCode"
    exit $winhomeExitCode
}

# Run verification tests (Pester)
Write-Host "Running Pester integration tests..."
$pesterResult = Invoke-Pester -Path ./verify.Tests.ps1 -Output Detailed -PassThru

if ($pesterResult.FailedCount -gt 0) {
    Write-Error "Pester tests failed with $($pesterResult.FailedCount) errors."
    exit 1
}

exit 0
