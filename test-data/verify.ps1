# verify.ps1

$ErrorActionPreference = "Stop"
$exitCode = 0

function Assert-True($condition, $message) {
    if (-not $condition) {
        Write-Error "Assertion failed: $message"
        $global:exitCode = 1
    } else {
        Write-Host "Assertion passed: $message"
    }
}

# 1. Check for installed application
try {
    $wingetList = winget list --id 7zip.7zip
    Assert-True ($wingetList -like "*7-Zip*"), "7-Zip should be installed"
} catch {
    Write-Error "Failed to check for 7-Zip installation."
    $global:exitCode = 1
}

# 2. Check for environment variable
$envVar = [Environment]::GetEnvironmentVariable("WINHOME_TEST", "User")
Assert-True ($envVar -eq "true"), "WINHOME_TEST environment variable should be set to 'true'"

# 3. Check for dotfile
# Assuming README.md is in the current directory (copied by Dockerfile)
$dotfileContent = Get-Content -Path "test-dotfile.md" -Raw
$readmeContent = Get-Content -Path "README.md" -Raw
Assert-True ($dotfileContent -eq $readmeContent), "Dotfile content should match README.md content"

exit $global:exitCode

