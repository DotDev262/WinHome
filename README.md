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

## 🔧 How It Works

WinHome reads a declarative `config.yaml` that defines your desired system state.
A built-in **Reconciliation Engine** compares it to the live system and ensures everything matches.

* Tracks system state in `winhome.state.json`
* Detects and corrects configuration drift
* Fully idempotent — run it once or 100 times: *the result is identical*

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

This roadmap helps collaborators, contributors, and recruiters understand the project’s trajectory.

### Core Features & System Integration
- [x] ~~Windows Services management~~
- [x] ~~Scheduled Tasks provisioning~~
- [x] ~~Add Chocolatey uninstall support~~
- [ ] Windows container support
- [ ] Hyper-V VM provisioning
- [ ] GPU acceleration toggle (for WSL)

### Developer Experience (DevEx)
- [ ] Automatic VSCode settings & extension sync
- [ ] Profile-based PATH overrides
- [ ] GUI mode for non-technical users
- [x] ~~State diff viewer (`--diff`)~~

### Advanced IaC & Tooling
- [ ] Local & remote module support
- [ ] Resource dependencies (`dependsOn:`)
- [ ] Generate function to generate the config file from a live system
- [ ] Domain Specific Language(DSL) similar to nix
- [ ] Integration tests for WSL installs
- [x] ~~Mocked tests for registry operations~~

**To be continued...** The roadmap is constantly evolving based on user feedback and new Windows capabilities!

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