# Configuration Reference

This document explains the `config.yaml` schema for WinHome.

## Secrets
WinHome supports referencing secrets from environment variables or local files using the `{{ }}` syntax.

*   `{{ env:VAR_NAME }}`: Replaced with the value of the environment variable.
*   `{{ file:C:\path\to\secret.txt }}`: Replaced with the trimmed content of the file.

**Example:**
```yaml
git:
  userEmail: "me@example.com"
  signingKey: "{{ env:GIT_SIGNING_KEY }}"
```

## Security Hardening
You can apply pre-defined security baselines using the `security_preset` key in `systemSettings`.

*   `baseline`: Enables SmartScreen, Disables Autorun/Autoplay, Disables LLMNR.
*   `strict`: Includes `baseline` + Disables Windows Script Host, Remote Assistance, and NetBIOS.

**Example:**
```yaml
systemSettings:
  security_preset: "baseline" # or "strict"
  dark_mode: true
```

## Plugins
You can configure installed plugins under the `extensions` key. The key name must match the plugin name.

**Example (Vim Plugin):**
```yaml
extensions:
  vim:
    settings:
      number: true
      relativenumber: true
      theme: "gruvbox"
```

## Apps
Install applications using supported managers (`winget`, `scoop`, `choco`) or installed plugins.

```yaml
apps:
  - id: "Microsoft.VisualStudioCode"
    manager: "winget"
  
  # Using a plugin
  - id: "tpope/vim-fugitive"
    manager: "vim"
```

