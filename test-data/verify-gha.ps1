# verify-gha.ps1

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
    # Check Winget app
    $wingetList = winget list --id 7zip.7zip -e
    Assert-True ($wingetList -like "*7-Zip*"), "7-Zip should be installed (Winget)"

} catch {
    Write-Error "Failed to check for application installations: $_"
    $global:exitCode = 1
}

# 2. Check for environment variable
$envVar = [Environment]::GetEnvironmentVariable("WINHOME_TEST_GHA", "User")
Assert-True ($envVar -eq "true"), "WINHOME_TEST_GHA environment variable should be set to 'true'"

# 3. Check for dotfile
$target = Join-Path $env:USERPROFILE "test-dotfile-gha.md"
$dotfileContent = Get-Content -Path $target -Raw
$readmeContent = Get-Content -Path "README.md" -Raw
Assert-True ($dotfileContent -eq $readmeContent), "Dotfile content should match README.md content"

# 4. Check for registry tweak
$regVal = Get-ItemPropertyValue -Path "HKCU:\Software\WinHomeGHA" -Name "GHATest" -ErrorAction SilentlyContinue
Assert-True ($regVal -eq 456), "Registry tweak should be set"

# 5. Check for system setting (show file extensions)
$hideFileExt = Get-ItemPropertyValue -Path "HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced" -Name "HideFileExt" -ErrorAction SilentlyContinue
Assert-True ($hideFileExt -eq 0), "Show file extensions should be enabled (HideFileExt=0)"

# 6. Check for git config
$gitName = git config --global user.name
Assert-True ($gitName -eq "WinHome GHA"), "Git user name should be set"

exit $global:exitCode
