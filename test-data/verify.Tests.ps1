# verify.Tests.ps1
$ErrorActionPreference = "Stop"

Describe "WinHome Integration Tests" {

    Context "Application Installation" {
        
        It "Should have 7-Zip installed via Scoop" {
            # Find Scoop
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
            
            # Check if Scoop is executable found
            $scoopExec | Should -Not -BeNullOrEmpty

            # List apps
            $scoopList = & $scoopExec list
            ($scoopList -join "`n") | Should -Match "7zip"
        }

        It "Should have Wget installed via Chocolatey" {
            # Find Choco
            $chocoExec = "choco"
            if (-not (Get-Command $chocoExec -ErrorAction SilentlyContinue)) {
                $chocoExec = "$env:ProgramData\chocolatey\bin\choco.exe"
            }

            # Check if Choco executable found
            $chocoExec | Should -Not -BeNullOrEmpty

            # List apps (limiting output to avoid huge logs)
            $chocoList = & $chocoExec list -l -r wget
            $chocoList | Should -Match "wget"
        }

        It "Should have Git installed via Winget" {
            # Winget might not be available in all test environments (e.g. some containers)
            if (Get-Command winget -ErrorAction SilentlyContinue) {
                $wingetList = winget list --id Git.Git -e
                $wingetList | Should -Match "Git"
            } else {
                Write-Warning "Winget not found, skipping Git installation check."
            }
        }
    }

    Context "Environment Configuration" {
        It "Should set the WINHOME_TEST environment variable" {
            $envVar = [Environment]::GetEnvironmentVariable("WINHOME_TEST", "User")
            if ($null -eq $envVar) {
                $envVar = [Environment]::GetEnvironmentVariable("WINHOME_TEST", "Machine")
            }
            $envVar | Should -Be "true"
        }
    }

    Context "Dotfiles" {
        It "Should sync the dotfile correctly" {
            $dotfileContent = Get-Content -Path "test-dotfile.md" -Raw
            $readmeContent = Get-Content -Path "README.md" -Raw
            $dotfileContent | Should -Be $readmeContent
        }
    }

    Context "Registry Tweaks" {
        It "Should apply the registry tweak" {
            $regVal = Get-ItemPropertyValue -Path "HKCU:\Software\WinHomeTest" -Name "TestValue" -ErrorAction SilentlyContinue
            $regVal | Should -Be 123
        }

        It "Should show file extensions (HideFileExt=0)" {
            $hideFileExt = Get-ItemPropertyValue -Path "HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced" -Name "HideFileExt" -ErrorAction SilentlyContinue
            $hideFileExt | Should -Be 0
        }
    }

    Context "Git Configuration" {
        It "Should configure Git user name" {
            $gitExec = "git"
            # Attempt to find git if not in path
            if (-not (Get-Command $gitExec -ErrorAction SilentlyContinue)) {
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
                $gitName = & $gitExec config --global user.name
                $gitName | Should -Be "WinHome Test"
            } else {
                Write-Warning "Git executable not found, skipping config check."
            }
        }
    }
}
