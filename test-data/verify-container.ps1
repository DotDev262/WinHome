# verify-container.ps1

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
    # Check for Scoop app
    $scoopExec = "scoop"
    if (-not (Get-Command $scoopExec -ErrorAction SilentlyContinue)) {
        $paths = @(
            "$env:USERPROFILE\scoop\shims\scoop.cmd",
            "$env:ProgramData\scoop\shims\scoop.cmd"
        )
        foreach ($p in $paths) {
            if (Test-Path $p) {
                $scoopExec = $p
                break
            }
        }
    }
    
    Write-Host "Using Scoop executable: $scoopExec"
    $scoopList = & $scoopExec list
    Assert-True ($scoopList -like "*7zip*"), "7-Zip should be installed (Scoop)"

    # Check for Choco app
    $chocoExec = "choco"
    if (-not (Get-Command $chocoExec -ErrorAction SilentlyContinue)) {
        $chocoExec = "$env:ProgramData\chocolatey\bin\choco.exe"
    }
    Write-Host "Using Choco executable: $chocoExec"
    $chocoList = & $chocoExec list -l -r wget
    Assert-True ($chocoList -like "*wget*"), "Wget should be installed (Choco)"

} catch {
    Write-Error "Failed to check for application installations: $_"
    $global:exitCode = 1
}

# 2. Check for environment variable
$envVar = [Environment]::GetEnvironmentVariable("WINHOME_TEST", "User")
if ($null -eq $envVar) {
    $envVar = [Environment]::GetEnvironmentVariable("WINHOME_TEST", "Machine")
}
Assert-True ($envVar -eq "true"), "WINHOME_TEST environment variable should be set to 'true'"

# 3. Check for dotfile
$dotfileContent = Get-Content -Path "test-dotfile.md" -Raw
$readmeContent = Get-Content -Path "README.md" -Raw
Assert-True ($dotfileContent -eq $readmeContent), "Dotfile content should match README.md content"

# 4. Check for registry tweak
$regVal = Get-ItemPropertyValue -Path "HKCU:\Software\WinHomeTest" -Name "TestValue" -ErrorAction SilentlyContinue
Assert-True ($regVal -eq 123), "Registry tweak should be set"

# 5. Check for system setting (show file extensions)
$hideFileExt = Get-ItemPropertyValue -Path "HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced" -Name "HideFileExt" -ErrorAction SilentlyContinue
Assert-True ($hideFileExt -eq 0), "Show file extensions should be enabled (HideFileExt=0)"

# 6. Check for git config
try {
    $gitExec = "git"
    if (-not (Get-Command $gitExec -ErrorAction SilentlyContinue)) {
        # Try common Scoop locations
        $paths = @(
            "$env:USERPROFILE\scoop\shims\git.exe",
            "$env:ProgramData\scoop\shims\git.exe",
            "$env:USERPROFILE\scoop\apps\git\current\cmd\git.exe",
            "$env:ProgramData\scoop\apps\git\current\cmd\git.exe"
        )
        foreach ($p in $paths) {
            if (Test-Path $p) {
                $gitExec = $p
                break
            }
        }
    }

    if ((Test-Path $gitExec) -or $gitExec -eq "git") {
        Write-Host "Using Git executable: $gitExec"
        $gitName = & $gitExec config --global user.name
        Assert-True ($gitName -eq "WinHome Test"), "Git user name should be set"
    } else {
         Write-Host "Git executable not found, skipping config check."
    }
} catch {
    Write-Error "Failed to check for Git configuration: $_"
    $global:exitCode = 1
}

exit $global:exitCode
