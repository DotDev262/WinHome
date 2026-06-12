# 🚀 Getting Started with WinHome

This guide will take you from zero to a working Windows configuration in under 10 minutes.

---

## Step 1 — Download WinHome

Run this in PowerShell as Administrator:

```powershell
Invoke-WebRequest -Uri "https://github.com/DotDev262/WinHome/releases/latest/download/WinHome.exe" -OutFile "WinHome.exe"
```

Optionally move it to your PATH for global access:

```powershell
Move-Item WinHome.exe "$env:USERPROFILE\bin\WinHome.exe"
```

---

## Step 2 — Create your first config.yaml

Create a file called `config.yaml` in the same folder as `WinHome.exe`:

```yaml
version: "1.0"

apps:
  - id: "Microsoft.PowerToys"
    manager: "winget"

registryTweaks:
  - path: "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"
    name: "HideFileExt"
    value: 0
    type: "dword"

envVars:
  - variable: "EDITOR"
    value: "notepad"
    action: "set"
```

This minimal config will:

- Install **PowerToys** via Winget
- Show **file extensions** in Explorer
- Set **EDITOR** environment variable to notepad

---

## Step 3 — Preview changes with --dry-run

Always run a dry-run first to see what WinHome will do without making any changes:

```powershell
.\WinHome.exe --dry-run
```

Review the output carefully before applying.

---

## Step 4 — Apply your configuration

Once happy with the dry-run output, apply the config:

```powershell
.\WinHome.exe
```

WinHome will compare your config to the live system and apply only what is needed.

---

## Step 5 — Verify the state

Check what WinHome has tracked:

```powershell
.\WinHome.exe --diff
```

This shows the current state versus your desired config. You can also run `winhome state list` to
view all currently tracked and managed items.

## ⚠️ Security Note

Never commit `config.yaml` to a public repository if it contains secrets, API tokens, or passwords.
Use a private repo or add it to `.gitignore`.

---

## Next Steps

Now that you have a working setup, explore more:

- [Configuration Reference](./config.md) — all available options
- [Configuration Cookbook](./cookbook.md) — real-world examples for Developer, Minimalist, and Gamer
  setups
- [Security Guide](./security.md) — best practices
- [Troubleshooting](./troubleshooting.md) — common issues and fixes
