<div align="center">
  
# 🪟 WinHome

<img src="./.github/banner.png" alt="WinHome Banner" width="80%">

A declarative, portable, idempotent **Infrastructure-as-Code tool for Windows**  
powered by modern, dependency-free, single-file .NET.

---

### 🔰 Project Badges

![Release](https://img.shields.io/github/v/release/DotDev262/WinHome?label=latest)
![Downloads](https://img.shields.io/github/downloads/DotDev262/WinHome/total?color=blue)
![Stars](https://img.shields.io/github/stars/DotDev262/WinHome?style=social)
![License](https://img.shields.io/github/license/DotDev262/WinHome)
![Platform](https://img.shields.io/badge/platform-Windows%2010%20%7C%2011-blue)
![.NET](https://img.shields.io/badge/.NET-10.0-purple)
![Build](https://img.shields.io/github/actions/workflow/status/DotDev262/WinHome/release.yaml?label=build)

</div>

---

## ⭐ Why WinHome?

I built WinHome to create a **lightweight, dependency-free configuration tool** that runs **natively on Windows** as a **single-file EXE** — no Python, Ruby, or agent installations required. This project was heavily inspired by NixOS's `home-manager` but tailored specifically for the Windows ecosystem.

WinHome focuses on the real needs of **Windows developers**, including:

- First-class **Winget** support  
- Deep **WSL** provisioning  
- Native **Registry tweaks** and system settings  

The goal is to make Windows environment automation as simple, fast, and reliable as possible.

---

## 🚀 Installation

WinHome ships as a **self-contained single EXE** (no .NET runtime needed), compatible with all Windows x64 systems.

1. Visit the **Releases Page**
2. Download **WinHome.exe**
3. Run it from PowerShell or CMD

### Quick Install (PowerShell)

```powershell
Invoke-WebRequest -Uri "https://github.com/DotDev262/WinHome/releases/latest/download/WinHome.exe" -OutFile "WinHome.exe"
````

---

## 🔧 How It Works & Configuration Wiki

WinHome reads a declarative `config.yaml` that defines your desired system state.
A built-in **Reconciliation Engine** compares it to the live system and ensures everything matches.

* Tracks system state in `winhome.state.json`
* Detects and corrects configuration drift
* Fully idempotent — run it once or 100 times: *the result is identical*

For a detailed breakdown of all configuration options, refer to the [Configuration Wiki](./docs/config.md).


---

## ✨ Features

### 📦 Universal Package Management

* Winget
* Scoop
* Chocolatey
* Mise

### 🐧 WSL Management

* Auto-install and configure distros
* Run post-provision scripts
* Kernel settings and version management

### 🔗 Dotfiles Sync

### ⚙️ System Configuration

### 🛡️ Safe Dry-Run Mode

### 🔄 Deterministic Idempotency

---

## 🗺️ Roadmap / Planned Features

This roadmap is a living document that outlines the project's future direction. It will be updated with new features and ideas as the project evolves.

### Core Features & System Integration
- [x] ~~Windows Services management~~
- [x] ~~Scheduled Tasks provisioning~~
- [x] ~~Add Chocolatey uninstall support~~
- [ ] **VSCode Integration**: Automatically sync settings and extensions.
- [ ] **Resource Dependencies**: Introduce a `dependsOn:` attribute to control execution order.
- [ ] **Plugin Architecture**: Redesign the core to support external providers for services and package managers.
- [ ] **Transactional Rollbacks**: Implement logic to automatically undo changes on a failed run.
- [ ] **Windows Container Support**: Add features for provisioning and managing Windows containers.
- [ ] **Hyper-V VM Provisioning**: Introduce capabilities for managing local Hyper-V virtual machines.

### Developer Experience (DevEx) & Tooling
- [x] ~~State diff viewer (`--diff`)~~
- [ ] **Configuration Schema Validation**: Validate `config.yaml` against a formal schema to provide better error messages.
- [ ] **Advanced State Management**: Add CLI commands to view, backup, and restore system state.
- [ ] **Structured Output**: Add a `--json` flag for machine-readable output of run results.
- [ ] **GUI Mode**: Develop a simple graphical user interface for non-technical users.
- [ ] **Profile-based PATH Overrides**: Allow different profiles to have unique PATH environment variables.
- [ ] **"Generate" Function**: Add a command to generate a `config.yaml` file from the current state of a live system.
- [ ] **DSL**: Evolve the configuration into a more powerful Domain-Specific Language (similar to Nix).

### Code Quality & Automation
- [x] ~~Mocked tests for registry operations~~
- [ ] **Containerized Acceptance Tests**: Build a full acceptance test suite that runs inside a clean Windows container.
- [ ] **Complete Unit Test Coverage**: Ensure all services and managers have comprehensive unit tests.
- [ ] **Publish Docs to GitHub Pages**: Automate the publishing of the `/docs` directory to a professional documentation website.
- [ ] **Automate Release Notes**: Use tools like `release-drafter` to auto-generate changelogs for new releases.
- [ ] **Formalize Contribution Process**: Create a `CONTRIBUTING.md` file and GitHub templates for issues and PRs.
- [ ] **Refactor Core Logic**: Decouple `Program.cs` and simplify the Dependency Injection setup.

## 📅 Version Roadmap

Here is a tentative plan for upcoming releases.

### v1.1 — The Quality & DX Release
*Focus: Internal refactoring, test coverage, and developer experience.*
- [ ] **Complete Unit Test Coverage**:
  - [x] `DotfileService`
  - [x] `WslService`
  - [x] `GitService`
  - [x] `EnvironmentService`
- [ ] **Refactor Core Logic**:
  - [ ] Simplify Dependency Injection in `Program.cs`.
  - [ ] Decouple `Program.cs` by moving logic into dedicated `CliBuilder` and `AppHost` classes.
- [ ] **Logging & Testability**:
  - [x] Introduce a proper `ILogger` service (Console/JSON).
  - [x] Support `WINHOME_CONFIG_PATH` environment variable.
  - [x] Implement distinct exit codes for automation.
- [ ] **Validation & Automation**:
  - [ ] Add Configuration Schema Validation (JSON Schema).
  - [x] Finalize Containerized Acceptance Test Suite.
- [x] **Formalize Contribution Process** (`CONTRIBUTING.md`, templates).

### v1.2 — The Core Features Release
*Focus: Adding highly-requested features for end-users.*
- [ ] **VSCode Integration** (Settings & Extension Sync).
- [ ] **Advanced State Management** (`state list`, `state backup`, `state restore`).
- [ ] **Automation**:
  - [ ] Publish Docs to GitHub Pages (DocFx).
  - [ ] Automate Release Notes (`release-drafter`).
- [ ] **Structured Output**: Finalize `--json` integration for all modules.

### v2.0 — The Architecture Release
*Focus: Major architectural changes to support long-term extensibility and power.*
- [ ] Introduce a full Plugin Architecture
- [ ] Implement Resource Dependencies (`dependsOn:`)
- [ ] Implement Transactional Rollbacks on failure
- [ ] Evolve configuration towards a true DSL

---

## 📅 Changelog

### Day 5: Containerization & Advanced Features (Part 1)
- [x] **Create a Test `Dockerfile`**: Developed a `Dockerfile` for Windows containers.

### Day 1: Basic Code Cleanup
- [x] **Split `Interfaces.cs`**: Moved each interface into its own file in `src/Interfaces/`.
- [x] **Split `Model.cs`**: Moved each model class into its own file in `src/Models/`.
- [x] **Remove Unused Files**: Deleted `tests/WinHome.Tests/UnitTest1.cs`.
- [x] **Review `.gitignore`**: Audited and improved the `.gitignore` file.

---

## 🏗️ Technical Architecture

Built with modern .NET engineering patterns:

* **Dependency Injection** (`Microsoft.Extensions.Hosting`)
* **Strategy Pattern** across package managers
* **Testable Core** via abstractions (Registry, FS, Processes)
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
  - id: "python@3.10"
    manager: "mise"

dotfiles:
  - src: "./files/.gitconfig"
    target: "~/.gitconfig"

envVars:
  - variable: "EDITOR"
    value: "nvim"
    action: "set"
  - variable: "Path"
    value: "%USERPROFILE%\\bin"
    action: "append"

registryTweaks:
  - path: "HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced"
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
```

---

## 🤝 Contributing

Contributions, discussions, and feature ideas are welcome!
Please open an Issue or Pull Request on GitHub.

---

## 🙏 This Is Possible Thanks To

WinHome stands on the shoulders of incredible open-source technologies:

* **Microsoft .NET**
* **Winget / Scoop / Chocolatey / Mise**
* **YAML**
* **GitHub Actions**
* **PowerShell**

And most importantly, the open-source community. ❤️

---

## 📄 License

Released under the **MIT License**.

---