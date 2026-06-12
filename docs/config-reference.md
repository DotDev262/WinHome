# Configuration Reference

This document explains the `config.yaml` schema for WinHome.

## Secrets

WinHome supports referencing secrets from environment variables or local files using the `{{ }}`
syntax.

- `{{ env:VAR_NAME }}`: Replaced with the value of the environment variable.
- `{{ file:C:\path\to\secret.txt }}`: Replaced with the trimmed content of the file.

**Example:**

```yaml
git:
  userEmail: 'me@example.com'
  signingKey: '{{ env:GIT_SIGNING_KEY }}'
```

## Security Hardening

You can apply pre-defined security baselines using the `security_preset` key in `systemSettings`.

- `baseline`: Enables SmartScreen, Disables Autorun/Autoplay, Disables LLMNR.
- `strict`: Includes `baseline` + Disables Windows Script Host, Remote Assistance, and NetBIOS.

**Example:**

```yaml
systemSettings:
  security_preset: 'baseline' # or "strict"
  dark_mode: true
```

## Power Management

You can configure system power and sleep timeouts under `systemSettings` (values are in minutes).

- `screen_timeout_ac`: Screen timeout when plugged in.
- `screen_timeout_dc`: Screen timeout on battery.
- `sleep_timeout_ac`: Sleep timeout when plugged in.
- `sleep_timeout_dc`: Sleep timeout on battery.

**Example:**

```yaml
systemSettings:
  screen_timeout_ac: 15
  screen_timeout_dc: 5
  sleep_timeout_ac: 60
  sleep_timeout_dc: 15
```

## Plugins

You can configure installed plugins under the `extensions` key. The key name must match the plugin
name.

**Example (Vim Plugin):**

```yaml
extensions:
  vim:
    settings:
      number: true
      relativenumber: true
      theme: 'gruvbox'
```

**Example (Obsidian Plugin):**

```yaml
extensions:
  obsidian:
    vaults:
      - path: "C:\\Users\\test\\Documents\\TestVault"
        settings:
          spellcheck: true
          accentColor: '#002aff'
        plugins:
          - 'obsidian-git'
```

**Example (PowerToys Plugin):**

```yaml
extensions:
  powertoys:
    general:
      settings:
        theme: 0
    modules:
      fancyzones:
        enabled: true
        settings:
          shiftDrag: true
      awake:
        enabled: true
        settings:
          keepAwake: true
          keepAwakeTimeInMinutes: 30
      powerrename:
        enabled: true
        settings:
          isEnabled: true
```

**Example (Oh My Posh Plugin):**

```yaml
ohmyposh:
  profile: "C:\\Users\\test\\Documents\\PowerShell\\Microsoft.PowerShell_profile.ps1"
  settings:
    theme: 'tokyonight'
```

## Apps

Install applications using supported managers (`winget`, `scoop`, `choco`) or installed plugins.

```yaml
apps:
  - id: 'Microsoft.VisualStudioCode'
    manager: 'winget'

  # Using a plugin
  - id: 'tpope/vim-fugitive'
    manager: 'vim'
```

## Profiles

Profiles can override selected top-level settings when `--profile <name>` is used.

```yaml
envVars:
  - variable: "EDITOR"
    value: "nvim"
    action: "set"

profiles:
  work:
    git:
      userName: "Work User"
      userEmail: "work@example.com"
    envVars:
      - variable: "EDITOR"
        value: "code"
        action: "set"
      - variable: "Path"
        value: "%USERPROFILE%\work\bin"
        action: "append"
```

For `profiles.<name>.envVars`, `action: set` replaces any matching top-level variable for that
profile. `action: append` keeps the base variable entries and adds the profile-specific value.
