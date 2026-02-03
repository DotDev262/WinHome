# run-test-container.ps1

# Setup plugins
$pluginDir = Join-Path $env:LOCALAPPDATA "WinHome\plugins"
$testPluginSrc = "tests\TestPluginJS"
$testPluginDest = Join-Path $pluginDir "test-echo-js"

if (Test-Path $testPluginSrc) {
    Write-Host "Setting up test plugin in $testPluginDest"
    New-Item -ItemType Directory -Force -Path $testPluginDest | Out-Null
    Copy-Item "$testPluginSrc\*" -Destination $testPluginDest -Recurse -Force
} else {
    Write-Warning "Test plugin source not found at $testPluginSrc"
}

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
