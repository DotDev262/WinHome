---
_disableBreadcrumb: true
---
<br />
<div align="center">
  <img src="https://raw.githubusercontent.com/DotDev262/WinHome/main/.github/banner.png" alt="WinHome Banner" width="80%">
</div>

# WinHome Documentation

WinHome is a declarative, portable, idempotent **Infrastructure-as-Code tool for Windows**.

### 🚀 [Get Started](docs/config.md) | 📘 [Configuration Reference](docs/config-reference.md)

---

## What is WinHome?

WinHome allows you to define your Windows environment in a simple `config.yaml` file and apply it to any machine. It handles:

*   **Package Managers**: Winget, Scoop, Chocolatey
*   **System Settings**: Registry tweaks, Windows Services, Scheduled Tasks
*   **Developer Tools**: WSL Distros, Git Configuration, Dotfiles
*   **Editor Setup**: VS Code Extensions/Settings, Neovim

---

## Quick Install

```powershell
Invoke-WebRequest -Uri "https://github.com/DotDev262/WinHome/releases/latest/download/WinHome.exe" -OutFile "WinHome.exe"
```

[View the Source on GitHub](https://github.com/DotDev262/WinHome)
