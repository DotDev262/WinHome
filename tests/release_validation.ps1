param(
    [string]$BinaryPath = ".\src\bin\Release\net10.0-windows\win-x64\WinHome.exe"
)

$ErrorActionPreference = "Stop"

function Assert-Result {
    param([bool]$Condition, [string]$Message)
    if ($Condition) {
        Write-Host "✅ [PASS] $Message" -ForegroundColor Green
    } else {
        Write-Host "❌ [FAIL] $Message" -ForegroundColor Red
        exit 1
    }
}

Write-Host "`n🔍 Starting Pre-Release Validation for WinHome v1.2.0...`n" -ForegroundColor Cyan

# 1. Validate Artifact Exists
if (-not (Test-Path $BinaryPath)) {
    Write-Host "❌ Artifact not found at: $BinaryPath" -ForegroundColor Red
    exit 1
}
Write-Host "✅ Artifact found." -ForegroundColor Green

# 2. Check Version
# Since there isn't an explicit --version flag implemented in the main command handling shown in Program.cs context,
# we will verify that the binary metadata is correct or if we can infer it.
# However, assuming standard .NET CLI behavior or that we added it implicitly, let's try to get it.
# If CLI doesn't support --version explicitly printing to stdout, we can check file version info.

try {
    $versionInfo = [System.Diagnostics.FileVersionInfo]::GetVersionInfo((Resolve-Path $BinaryPath).Path)
    $version = $versionInfo.FileVersion
    Write-Host "   Detected Binary Version: $version"
    Assert-Result ($version -eq "1.2.0.0") "Binary FileVersion is 1.2.0.0"
} catch {
    Assert-Result $false "Failed to read binary version info"
}

# 3. Dry Run with Dummy Config
$configFile = "test_release_dummy.yaml"
@"
version: "1.0"
apps:
  - id: "WinHome.Validation"
    manager: "winget"
"@ | Set-Content $configFile

Write-Host "   Running Dry-Run..."
try {
    $process = Start-Process -FilePath $BinaryPath -ArgumentList "--config $configFile --dry-run" -NoNewWindow -PassThru -Wait
    Assert-Result ($process.ExitCode -eq 0) "Dry-run process exited with code 0"
} catch {
    Assert-Result $false "Dry-run execution failed"
}

# Cleanup
Remove-Item $configFile -ErrorAction SilentlyContinue

Write-Host "`n✅ VALIDATION PASSED" -ForegroundColor Green