# 🍳 Configuration Cookbook

New to WinHome? Start here! This cookbook provides complete, production-ready `config.yaml`
templates tailored for different use cases. You can copy these configurations, modify the fields
(such as usernames and emails), and apply them directly to your system.

---

## 💻 1. The Developer Setup

This configuration is designed for full-stack developers, DevOps engineers, and power users. It
configures a complete development environment, including development tools, global Git settings, VS
Code extension management, a customized Neovim environment, user-level environment variables, and a
WSL (Windows Subsystem for Linux) instance.

```yaml
version: "1.0"

# 📦 Universal Package Management
apps:
  # Development Utilities & Environments
  - id: "Git.Git"
    manager: "winget"
  - id: "Microsoft.VisualStudioCode"
    manager: "winget"
  - id: "Docker.DockerDesktop"
    manager: "winget"
  - id: "Nodejs.Nodejs.LTS"
    manager: "winget"
  - id: "Python.Python.3.11"
    manager: "winget"
  - id: "neovim"
    manager: "scoop"
  - id: "JanDeDobbeleer.OhMyPosh"
    manager: "winget"

# 🔗 Dotfiles Sync
dotfiles:
  - src: "./dotfiles/.gitconfig"
    target: "~/.gitconfig"
  - src: "./dotfiles/.wslconfig"
    target: "~/.wslconfig"

# ⚙️ Global Git Configuration
git:
  userName: "Developer Name"
  userEmail: "dev@example.com"
  signingKey: "{{ env:GIT_SIGNING_KEY }}" # Reference secret from environment variables
  commitGpgSign: true
  settings:
    init.defaultBranch: "main"
    pull.rebase: "true"
    core.editor: "code --wait"
    core.autocrlf: "true"

# 🖥️ User Environment Variables
envVars:
  - variable: "EDITOR"
    value: "nvim"
    action: "set"
  - variable: "GOPATH"
    value: "%USERPROFILE%\go"
    action: "set"
  - variable: "Path"
    value: "%USERPROFILE%\go\bin"
    action: "append"

# ⚙️ VS Code Settings and Extension Sync
vscode:
  extensions:
    - "dbaeumer.vscode-eslint"
    - "esbenp.prettier-vscode"
    - "eamodio.gitlens"
    - "golang.go"
  settings:
    "editor.tabSize": 2
    "editor.insertSpaces": true
    "editor.formatOnSave": true
    "files.autoSave": "afterDelay"
    "workbench.colorTheme": "Default Dark Modern"

# 📝 Neovim Custom Settings & Extensions
vim:
  extensions:
    - "tpope/vim-commentary"
    - "nvim-treesitter/nvim-treesitter"
    - "morhetz/gruvbox"
  settings:
    number: true
    relativenumber: true
    theme: "gruvbox"

# 🐧 WSL Distro Provisioning
wsl:
  update: true
  defaultVersion: 2
  defaultDistro: "Ubuntu-22.04"
  distros:
    - name: "Ubuntu-22.04"
      setupScript: "scripts/wsl_dev_init.sh" # Execute a custom post-provision script

# 🎛️ System Settings (Registry Tweaks)
systemSettings:
  dark_mode: true
  taskbar_alignment: "left"
  show_file_extensions: true
  show_hidden_files: true
  seconds_in_clock: true
  explorer_launch_to: "this_pc"
  bing_search_enabled: false
```

---

## 🧹 2. The Minimalist Setup

This setup is tailored for users who prefer a clean, minimal desktop environment with maximum
privacy and minimal background bloat. It applies security baseline hardening, optimizes essential
Explorer settings, installs a few core utilities, and disables heavy or unnecessary Windows
services.

```yaml
version: '1.0'

# 📦 Universal Package Management
apps:
  - id: 'Git.Git'
    manager: 'winget'
  - id: '7zip.7zip'
    manager: 'winget'
  - id: 'Microsoft.PowerToys'
    manager: 'winget'

# 🎛️ Clean & Secure System Settings
systemSettings:
  # Enable pre-defined security baseline (enables SmartScreen, disables Autorun/Autoplay)
  security_preset: 'baseline'
  dark_mode: true
  taskbar_alignment: 'center'
  taskbar_widgets: 'hide' # Keep the taskbar clear of widgets
  show_file_extensions: true
  show_hidden_files: true
  explorer_launch_to: 'this_pc' # Launch File Explorer directly to This PC
  bing_search_enabled: false # Disable web search results in Start Menu

# ⚙️ Global Git Configuration
git:
  userName: 'Minimal User'
  userEmail: 'minimalist@example.com'
  settings:
    init.defaultBranch: 'main'

# 🚫 De-bloat System Services
services:
  # Disable heavy background telemetry and services that aren't needed
  - name: 'Spooler' # Print Spooler (Safe to disable if you don't print)
    state: 'stopped'
    startup: 'disabled'
  - name: 'Fax' # Legacy fax service
    state: 'stopped'
    startup: 'disabled'
  - name: 'DiagTrack' # Connected User Experiences and Telemetry
    state: 'stopped'
    startup: 'disabled'
```

---

## 🎮 3. The Gamer Setup

Tailored for pc gamers who want to prioritize gaming performance, install gaming platforms, and
eliminate system-level distractions. It optimizes mouse precision registry tweaks, disables
latency-inducing Windows Game DVR telemetry, sets custom hardware parameters, and installs popular
gaming tools.

```yaml
version: "1.0"

# 📦 Gaming Software & Launchers
apps:
  - id: "Valve.Steam"
    manager: "winget"
  - id: "Discord.Discord"
    manager: "winget"
  - id: "EpicGames.EpicGamesLauncher"
    manager: "winget"
  - id: "Microsoft.XboxApp"
    manager: "winget"

# 🛠️ Gaming Performance Registry Tweaks
registryTweaks:
  # Disable Mouse Acceleration (Enhance Pointer Precision) for raw input
  - path: "HKCU\Control Panel\Mouse"
    name: "MouseSpeed"
    value: "0"
    type: "string"
  - path: "HKCU\Control Panel\Mouse"
    name: "MouseThreshold1"
    value: "0"
    type: "string"
  - path: "HKCU\Control Panel\Mouse"
    name: "MouseThreshold2"
    value: "0"
    type: "string"

  # Disable Windows Game DVR background recording telemetry to prevent stutters
  - path: "HKCU\System\GameConfigStore"
    name: "GameDVR_Enabled"
    value: 0
    type: "dword"
  - path: "HKLM\SOFTWARE\Policies\Microsoft\Windows\GameDVR"
    name: "AllowGameDVR"
    value: 0
    type: "dword"

# 🎛️ Hardware & Environment Tweaks
systemSettings:
  dark_mode: true
  taskbar_alignment: "center"
  taskbar_widgets: "hide"
  brightness: 100                 # Maximum brightness for clear visibility
  volume: 75                      # Set system sound level
  notification:
    title: "WinHome Gaming Setup Applied"
    message: "Launchers installed, mouse raw-input active, and performance tweaks applied!"
```

---

> [!TIP] **Using Custom Variables:** Remember that you can reference secrets, files, or environment
> variables in your templates by using the `{{ env:VARIABLE_NAME }}` or
> `{{ file:C:\path\to\file.txt }}` syntax. This helps keep your production `config.yaml` secure and
> clean.
