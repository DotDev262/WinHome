# run-test-container.ps1

# Setup plugins
$pluginDir = Join-Path $env:LOCALAPPDATA "WinHome\plugins"
$testPluginSrc = "tests\TestPluginJS"
$testPluginPySrc = "tests\TestPlugin"
$vimPluginSrc = "plugins\vim"
$vscodePluginSrc = "plugins\vscode"

function Copy-Plugin($src, $name) {
    if (Test-Path $src) {
        $dest = Join-Path $pluginDir $name
        Write-Host "Setting up plugin $name in $dest"
        New-Item -ItemType Directory -Force -Path $dest | Out-Null
        Copy-Item "$src\*" -Destination $dest -Recurse -Force
    } else {
        Write-Warning "Plugin source not found at $src"
    }
}

Copy-Plugin $testPluginSrc "test-echo-js"
Copy-Plugin $testPluginPySrc "test-echo-py"
Copy-Plugin $vimPluginSrc "vim"
Copy-Plugin $vscodePluginSrc "vscode"

# Run WinHome
./WinHome.exe --config test-config-container.yaml --debug
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
