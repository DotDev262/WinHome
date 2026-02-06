# setup-sandbox.ps1
$ErrorActionPreference = "Stop"

# 1. Upgrade Pester
Write-Host "Upgrading Pester..." -ForegroundColor Yellow
powershell -Command "Install-PackageProvider -Name NuGet -MinimumVersion 2.8.5.201 -Force -Scope CurrentUser; Install-Module -Name Pester -Force -SkipPublisherCheck -Scope CurrentUser"

$pluginDir = Join-Path $env:LOCALAPPDATA "WinHome\plugins"
Write-Host "Setting up plugins in $pluginDir..." -ForegroundColor Cyan

function Copy-Plugin($src, $name) {
    if (Test-Path $src) {
        $dest = Join-Path $pluginDir $name
        Write-Host "  -> $name"
        New-Item -ItemType Directory -Force -Path $dest | Out-Null
        Copy-Item "$src\*" -Destination $dest -Recurse -Force
    }
}

Copy-Plugin "../../plugins\vim" "vim"
Copy-Plugin "../../plugins\vscode" "vscode"
Copy-Plugin "../../tests\TestPluginJS" "test-echo-js"
Copy-Plugin "../../tests\TestPlugin" "test-echo-py"

# 2. Install Vim and VSCode using WinHome
$winhomeExe = "../../publish/WinHome.exe"
$env:WINHOME_STATE_PATH = Join-Path $env:TEMP "winhome.sandbox.state.json"

if (Test-Path $winhomeExe) {
    Write-Host "Bootstrapping sandbox tools (Vim, VSCode)..." -ForegroundColor Yellow
    & $winhomeExe --config ../../test-data/bootstrap-sandbox.yaml
} else {
    Write-Warning "WinHome.exe not found at $winhomeExe. Skipping tool bootstrap."
}

Write-Host "Setup complete!" -ForegroundColor Green
