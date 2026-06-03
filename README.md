<div align="center">
  
# 🪟 WinHome

<img src="./.github/banner.png" alt="WinHome Banner" width="80%">

A declarative, portable, idempotent **Infrastructure-as-Code tool for Windows**  
powered by modern, dependency-free, single-file .NET.

---

### 🔰 Project Badges

[![CI](https://img.shields.io/github/actions/workflow/status/DotDev262/WinHome/ci.yml?branch=main&label=CI&logo=github)](https://github.com/DotDev262/WinHome/actions/workflows/ci.yml)
[![Release Build](https://img.shields.io/github/actions/workflow/status/DotDev262/WinHome/release.yaml?label=release%20build&logo=github)](https://github.com/DotDev262/WinHome/actions/workflows/release.yaml)
[![Docs](https://img.shields.io/github/actions/workflow/status/DotDev262/WinHome/docs.yml?label=docs&logo=github-pages&color=blue)](https://github.com/DotDev262/WinHome/actions/workflows/docs.yml)
[![Code Coverage](https://img.shields.io/badge/coverage-see%20CI-lightgrey?logo=codecov)](https://github.com/DotDev262/WinHome/actions/workflows/ci.yml)
[![Release](https://img.shields.io/github/v/release/DotDev262/WinHome?label=latest&logo=github)](https://github.com/DotDev262/WinHome/releases/latest)
[![Downloads](https://img.shields.io/github/downloads/DotDev262/WinHome/total?color=blue&logo=github)](https://github.com/DotDev262/WinHome/releases)
[![Stars](https://img.shields.io/github/stars/DotDev262/WinHome?style=social)](https://github.com/DotDev262/WinHome/stargazers)
[![License](https://img.shields.io/github/license/DotDev262/WinHome)](./LICENSE)
[![Platform](https://img.shields.io/badge/platform-Windows%2010%20%7C%2011-0078D4?logo=windows)]()
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)

</div>

---

## 💬 Community & Support

*   **GitHub Discussions:** The best place for "How do I...?" questions and community support. **[Join the conversation!](https://github.com/DotDev262/WinHome/discussions)**
*   **Issues:** For bug reports and feature requests. **[Open a new issue using our templates.](https://github.com/DotDev262/WinHome/issues/new/choose)**
*   **GSSOC 2026:** Participants should check our **[Contributing Guide](./CONTRIBUTING.md)** for program-specific instructions.

---

I built WinHome to create a **lightweight, dependency-free configuration tool** that runs **natively on Windows** as a **single-file EXE** — no Python, Ruby, or agent installations required. This project was heavily inspired by NixOS's `home-manager` but tailored specifically for the Windows ecosystem.

WinHome focuses on the real needs of **Windows developers**, including:

- First-class **Winget** support  
- Deep **WSL** provisioning  
- Native **Registry tweaks** and system settings  

The goal is to make Windows environment automation as simple, fast, and reliable as possible.

---

## 📋 Prerequisites

Before using WinHome, ensure your environment meets the following requirements:

*   **Operating System:** Windows 10 (Version 1809 or higher) or Windows 11.
*   **Privileges:** Must be executed with **Administrator** privileges to modify system settings and install packages.
*   **Internet Connection:** Required for downloading packages and updates.

---

## 🚀 Installation

WinHome ships as a **self-contained single EXE** (no .NET runtime needed), compatible with all Windows x64 systems.

### Method 1: Manual Download

1. Visit the [**Releases Page**](https://github.com/DotDev262/WinHome/releases/latest)
2. Download **WinHome.exe**
3. Run it from PowerShell or CMD

### Method 2: Quick Install (PowerShell One-Liner)

```powershell
Invoke-WebRequest -Uri "https://github.com/DotDev262/WinHome/releases/latest/download/WinHome.exe" -OutFile "WinHome.exe"
```

### Method 3: winget *(coming soon)*

> Once WinHome is published to the Windows Package Manager repository, you will be able to install it with:

```powershell
# Coming soon — pending winget submission
winget install DotDev262.WinHome
```

### Method 4: Scoop *(coming soon)*

> Once the Scoop bucket is published:

```powershell
# Coming soon — pending Scoop bucket submission
scoop bucket add winhome https://github.com/DotDev262/scoop-winhome
scoop install winhome
```

### Method 5: Chocolatey *(coming soon)*

> Once the Chocolatey package is published:

```powershell
# Coming soon — pending Chocolatey package approval
choco install winhome
```

> **Post-Install Note:** For easier global access, we recommend moving `WinHome.exe` to a folder included in your system's `PATH` environment variable (e.g., `C:\Users\<User>\bin`).

---

## 🔧 How It Works & Configuration Wiki

WinHome reads a declarative `config.yaml` that defines your desired system state.
A built-in **Reconciliation Engine** compares it to the live system and ensures everything matches.

* Tracks system state in `winhome.state.json`
* Detects and corrects configuration drift
* Fully idempotent — run it once or 100 times: *the result is identical*

For a detailed breakdown of all configuration options, refer to the [Configuration Wiki](./docs/config.md).

For complete, real-world setup examples (Developer, Minimalist, Gamer), see the [Configuration Cookbook](./docs/cookbook.md).

### ⚠️ Secrets & Security Warning

**Do not commit `config.yaml` to public repositories** if it contains sensitive information such as API tokens, passwords, or private environment variables. We recommend using a private repository or `.gitignore` for configurations containing secrets.

---

## ✨ Features

### 📦 Universal Package Management

* Winget (with auto-install support)
* Scoop
* Chocolatey

### 🐧 WSL Management

* Auto-install and configure distros
* Run post-provision scripts
* Kernel settings and version management

### 🔗 Dotfiles Sync

### ⚙️ System Configuration

### 🛡️ Safe Dry-Run Mode

### 🔄 Deterministic Idempotency

---

## 🛡️ Security & Reliability

WinHome implements enterprise-grade security controls to prevent common infrastructure automation pitfalls.


### 🔒 Context Awareness (RegistryGuard)
WinHome actively detects if it is running as `SYSTEM` (common in CI/CD or Scheduled Tasks) and **blocks attempts to modify `HKEY_CURRENT_USER`**. This prevents the "Admin Context Trap" where settings are accidentally applied to the LocalSystem profile instead of the logged-in user.

### 💾 Crash Resilience (Write-Through State)
The state engine uses a **Write-Through** pattern. Every successful action (e.g., installing an app, applying a registry key) is immediately flushed to disk (`winhome.state.json`). If the process crashes or is terminated (Ctrl+C), your progress is saved, and the next run will resume correctly without "zombie" state issues.

### 📦 Plugin Sandboxing
External plugins (Bun, Uv) run in a sandboxed process with strict limits:
*   **Memory Limit:** 10MB max output buffer to prevent OOM attacks.
*   **Time Limit:** 30-second execution timeout to prevent hangs.
*   **Isolation:** Plugins communicate strictly via JSON over Stdin/Stdout.

---

## 🗺️ Roadmap / Planned Features

This roadmap is a living document that outlines the project's future direction. It will be updated with new features and ideas as the project evolves.

### Core Features & System Integration
- [x] ~~Windows Services management~~
- [x] ~~Scheduled Tasks provisioning~~
- [x] ~~Add Chocolatey uninstall support~~
- [x] **Winget Auto-Install**: Automatically installs Winget if missing.
- [x] **Plugin Architecture**: Redesign the core to support external providers for services and package managers.
- [x] **VSCode Plugin**: Automatically sync settings and extensions using the new plugin architecture.
- [ ] **Resource Dependencies**: Introduce a `dependsOn:` attribute to control execution order.
- [ ] **Transactional Rollbacks**: Implement logic to automatically undo changes on a failed run.
- [ ] **Windows Container Support**: Add features for provisioning and managing Windows containers.
- [ ] **Hyper-V VM Provisioning**: Introduce capabilities for managing local Hyper-V virtual machines.

### Developer Experience (DevEx) & Tooling
- [x] ~~State diff viewer (`--diff`)~~
- [x] **Enhanced Logging**: Filtered, real-time output for package managers.
- [x] **Configuration Schema Validation**: Validate `config.yaml` against a formal schema to provide better error messages.
- [ ] **Advanced State Management**: Add CLI commands to view, backup, and restore system state.
- [ ] **Structured Output**: Add a `--json` flag for machine-readable output of run results.
- [ ] **GUI Mode**: Develop a simple graphical user interface for non-technical users.
- [x] **Profile-based PATH Overrides**: Allow different profiles to have unique PATH environment variables.
- [ ] **"Generate" Function**: Add a command to generate a `config.yaml` file from the current state of a live system.
- [ ] **DSL**: Evolve the configuration into a more powerful Domain-Specific Language (similar to Nix).

### Code Quality & Automation
- [x] ~~Mocked tests for registry operations~~
- [x] **Containerized Acceptance Tests**: Build a full acceptance test suite that runs inside a clean Windows container.
- [x] **Native GitHub Actions Testing**: End-to-end testing on real Windows VMs.
- [x] **Complete Unit Test Coverage**: Ensure all services and managers have comprehensive unit tests.
- [x] **Refactor Core Logic**: Decouple `Program.cs` and simplify the Dependency Injection setup.
- [ ] **Publish Docs to GitHub Pages**: Automate the publishing of the `/docs` directory to a professional documentation website.
- [ ] **Automate Release Notes**: Use tools like `release-drafter` to auto-generate changelogs for new releases.
- [ ] **Formalize Contribution Process**: Create a `CONTRIBUTING.md` file and GitHub templates for issues and PRs.

## 📅 Version Roadmap

Here is a tentative plan for upcoming releases.

### v1.1 — The Quality & DX Release
*Focus: Internal refactoring, test coverage, and developer experience.*
- [x] **Complete Unit Test Coverage**:
  - [x] `DotfileService`
  - [x] `WslService`
  - [x] `GitService`
  - [x] `EnvironmentService`
  - [x] `WingetService`
  - [x] `ScoopService`
  - [x] `ChocolateyService`
  - [x] `RegistryService`
  - [x] `SystemSettingsService`
  - [x] `ScheduledTaskService`
  - [x] `WindowsServiceManager`
- [x] **Refactor Core Logic**:
  - [x] Simplify Dependency Injection in `Program.cs`.
  - [x] Decouple `Program.cs` by moving logic into dedicated `CliBuilder` and `AppHost` classes.
- [x] **Logging & Testability**:
  - [x] Introduce a proper `ILogger` service (Console/JSON).
  - [x] Support `WINHOME_CONFIG_PATH` environment variable.
  - [x] Implement distinct exit codes for automation.
- [x] **Validation & Automation**:
  - [x] Add Configuration Schema Validation (JSON Schema).
  - [x] Finalize Containerized Acceptance Test Suite.
- [x] **Formalize Contribution Process** (`CONTRIBUTING.md`, templates).

### v1.2 — The Plugins & Extensibility Release
*Focus: Redesigning the core for extensibility and adding community-requested features.*
- [x] **Plugin Architecture**: Redesign the core to support external providers for services and package managers.
- [x] **VSCode Plugin**: Implement VSCode settings and extension sync as the first official plugin.
- [x] **Vim/Neovim Plugin**: Configure plugins and settings for Vim/Neovim.
- [x] **Obsidian Plugin**: Manage settings and community plugins across your Obsidian vaults.
- [x] **Oh My Posh Plugin**: Manage PowerShell terminal prompt themes.
- [x] **Config Generator (`winhome generate`)**: Scan the system and create a `config.yaml` based on installed apps and settings.
- [x] **Advanced State Management** (`state list`, `state backup`, `state restore`).
- [x] **Secret Reference Logic**: Add support for referencing secrets from environment variables or secure vaults.
- [x] **Self-Update Mechanism**: Allow `WinHome` to update itself to the latest version.
- [x] **Security Hardening Presets**: Add pre-defined configurations for locking down Windows security settings.
- [x] **Automation**:
  - [x] Publish Docs to GitHub Pages (DocFx).
  - [x] Automate Release Notes (`release-drafter`).
- [x] **Structured Output**: Finalize `--json` integration for all modules.

### v2.0 — The Architecture Release
*Focus: Major architectural changes to support long-term power and reliability.*
- [ ] Implement Resource Dependencies (`dependsOn:`)
- [ ] Implement Transactional Rollbacks on failure
- [ ] Evolve configuration towards a true DSL

---

## 🏗️ Technical Architecture

Built with modern .NET engineering patterns:

* **Dependency Injection** (`Microsoft.Extensions.Hosting`)
* **Strategy Pattern** across package managers
* **Testable Core** via abstractions (Registry, FS, Processes)
* **Cross-Platform Dev**: Can be developed/unit-tested on Linux & macOS.
* **CI/CD** via GitHub Actions (SingleFile & Native builds)

---

## 📘 Usage

```
.\WinHome.exe [options]
```

### Options

* `--config <path>`
* `--dry-run`, `-d`
* `--profile <name>`
* `--debug`
* `--diff`

---

## 🧩 Configuration Example (`config.yaml`)

```yaml
version: "1.0"

apps:
  - id: "Microsoft.PowerToys"
    manager: "winget"
  - id: "neovim"
    manager: "scoop"

dotfiles:
  - src: "./files/.gitconfig"
    target: "~/.gitconfig"

envVars:
  - variable: "EDITOR"
    value: "nvim"
    action: "set"
  - variable: "Path"
    value: "%USERPROFILE%\bin"
    action: "append"

registryTweaks:
  - path: "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"
    name: "HideFileExt"
    value: 0
    type: "dword"

systemSettings:
  showFileExtensions: true
  darkMode: true
  taskbarAlignment: "left"

git:
  userName: "John Doe"
  userEmail: "john.doe@example.com"
  settings:
    "core.editor": "code --wait"
    "init.defaultBranch": "main"

wsl:
  update: true
  defaultDistro: "Debian"
  defaultVersion: 2
  distros:
    - name: "Ubuntu-20.04"
    - name: "Debian"

profiles:
  work:
    git:
      userName: "John Doe (Work)"
      userEmail: "john.doe@work.com"
    envVars:
      - variable: "EDITOR"
        value: "code"
        action: "set"
      - variable: "Path"
        value: "%USERPROFILE%\work\bin"
        action: "append"
```

---

## 🗑️ Uninstallation

WinHome is fully portable. To uninstall it:

1.  Delete the `WinHome.exe` file.
2.  Delete the state file `winhome.state.json` (located in the same directory).

No registry keys or hidden folders are left behind by the tool itself.

---

## ❓ Troubleshooting
For a comprehensive troubleshooting guide, see [Troubleshooting](docs/troubleshooting.md).

**"Winget not recognized"**
> Ensure the **App Installer** is updated from the Microsoft Store. WinHome attempts to use the system-level Winget.

**PowerShell Script Errors**
> If you encounter execution policy errors, try running `Set-ExecutionPolicy RemoteSigned -Scope CurrentUser` in an administrative PowerShell window.

---

## 🤝 Contributing

Contributions, discussions, and feature ideas are welcome! WinHome is an open-source project and we'd love your help to make it better.

### 📖 Contribution Resources

| Resource | Description |
|---|---|
| [Contributing Guide](./CONTRIBUTING.md) | Full guide: branching, commit style, PR process |
| [GSSOC 2026 Instructions](./CONTRIBUTING.md#-gssoc-2026-participants) | Program-specific labels & tracking info |
| [Configuration Wiki](./docs/config.md) | Understand WinHome's configuration schema |
| [Configuration Cookbook](./docs/cookbook.md) | Real-world `config.yaml` examples |
| [Testing Guide](./docs/testing.md) | How to write and run tests |
| [Cross-Platform Dev](./docs/cross-platform-dev.md) | Developing on Linux/macOS |
| [Security Guide](./docs/security.md) | Security practices and guidelines |
| [Troubleshooting](./docs/troubleshooting.md) | Common issues and solutions |
| [GitHub Discussions](https://github.com/DotDev262/WinHome/discussions) | Community Q&A and ideas |
| [Open Issues](https://github.com/DotDev262/WinHome/issues) | Find something to work on |

### 🚀 Quick Start for Contributors

1. **Fork** the repository → [Fork WinHome](https://github.com/DotDev262/WinHome/fork)
2. **Clone** your fork and create a branch: `git checkout -b <issue-number>-short-description`
3. **Build**: `dotnet build WinHome.sln`
4. **Test**: `dotnet test WinHome.sln`
5. **Format**: `dotnet format WinHome.sln` (required before submitting)
6. **Open a Pull Request** and link it to the relevant issue (`Closes #<issue-number>`)

> 💡 **GSSOC 2026 participants:** Please read the [Contributing Guide](./CONTRIBUTING.md) for program-specific instructions on PR labels and leaderboard tracking.

Please open an [Issue](https://github.com/DotDev262/WinHome/issues/new/choose) or [Pull Request](https://github.com/DotDev262/WinHome/pulls) on GitHub.

<!-- ALL-CONTRIBUTORS-BADGE:START - Do not remove or modify this section -->
<!-- ALL-CONTRIBUTORS-BADGE:END -->

### ✨ Contributors

Thanks goes to these wonderful people ([emoji key](https://allcontributors.org/docs/en/emoji-key)):

<!-- ALL-CONTRIBUTORS-LIST:START - Do not remove or modify this section -->
<!-- markdownlint-disable -->
<table>
  <tbody>
    <tr>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/DotDev262"><img src="https://github.com/DotDev262.png" width="100px;" alt="DotDev262"/><br /><sub><b>DotDev262</b></sub></a><br /><a href="https://github.com/DotDev262/WinHome/commits?author=DotDev262" title="Code">💻</a> <a href="#design-DotDev262" title="Design">🎨</a> <a href="#ideas-DotDev262" title="Ideas">🤔</a> <a href="https://github.com/DotDev262/WinHome/commits?author=DotDev262" title="Documentation">📖</a></td>
    </tr>
  </tbody>
</table>

<!-- markdownlint-restore -->
<!-- ALL-CONTRIBUTORS-LIST:END -->

This project follows the [all-contributors](https://github.com/all-contributors/all-contributors) specification. Contributions of any kind welcome!

---

## 🙏 This Is Possible Thanks To

WinHome stands on the shoulders of incredible open-source technologies:

* **Microsoft .NET**
* **Winget / Scoop / Chocolatey**
* **YAML**
* **GitHub Actions**
* **PowerShell**

And most importantly, the open-source community. ❤️


📘 For detailed security guidance and best practices, see the [Security Guide](./docs/security.md).


---

## 📄 License

Released under the **MIT License**.
