# Troubleshooting

This guide helps you diagnose and fix common issues with WinHome.

## Common config.yaml Parsing Errors

- **Indentation errors:** YAML is indentation-sensitive. Always use spaces, never tabs.
- **Missing quotes:** String values containing special characters should be wrapped in quotes.
- **Invalid values:** Ensure values match the expected type (e.g., `true`/`false` for booleans,
  numbers for dword values).

Example of a correct entry:

```yaml
systemSettings:
  darkMode: true
  showFileExtensions: true
```

If WinHome fails to parse your config, it will print the line number and reason. Double-check that
line and the lines around it.

---

## Registry Permission Issues

WinHome modifies registry keys under `HKCU` (HKEY_CURRENT_USER). Common issues:

- **Running as SYSTEM:** WinHome's RegistryGuard actively blocks HKCU modifications when running as
  SYSTEM (e.g., in CI/CD pipelines or Scheduled Tasks) to prevent the Admin Context Trap. Always run
  WinHome as your actual user account, not as SYSTEM.
- **Access Denied:** Ensure WinHome is run with Administrator privileges. Right-click the EXE and
  select "Run as administrator".

---

## Network Failures During Bootstrapping

- **Winget not recognized:** Ensure the App Installer is updated from the Microsoft Store. WinHome
  attempts to use the system-level Winget automatically.
- **Download failures:** Check your internet connection. Corporate firewalls or proxies may block
  package manager endpoints.
- **Scoop/Chocolatey errors:** Ensure PowerShell execution policy allows scripts. Run the following
  in an administrative PowerShell window:

```powershell
Set-ExecutionPolicy RemoteSigned -Scope CurrentUser
```

---

## WSL Installation Problems

- **WSL not enabling:** Ensure virtualization is enabled in your BIOS/UEFI settings.
- **Distro not found:** Verify the distro name in your `config.yaml` matches exactly what is
  available via `wsl --list --online`.
- **Kernel version issues:** Run `wsl --update` manually to ensure your WSL kernel is up to date
  before running WinHome.

---

## Plugin Runtime Resolution Issues

WinHome sandboxes external plugins (Bun, Uv) with strict limits:

- **10MB max output buffer** — plugins producing excessive output will be terminated.
- **30-second timeout** — plugins that hang will be killed automatically.

If a plugin fails:

- Check that the plugin runtime (e.g., Bun, Uv) is installed and accessible in your PATH.
- Run WinHome with `--debug` to see the exact plugin command being executed and its output.

---

## Enabling Verbose/Debug Logging

Run WinHome with the `--debug` flag for detailed output:

```powershell
.\WinHome.exe --debug
```

This will print every action WinHome takes, including registry reads/writes, package manager calls,
and plugin executions.

---

## How to Read Log Output

WinHome prints logs in this general format:

- **INFO** — Normal operations (e.g., installing a package, applying a registry key).
- **WARN** — Non-fatal issues that may need attention.
- **ERROR** — Something failed. Check the message for the affected resource and reason.

Progress is saved to `winhome.state.json` after every successful action. If WinHome crashes, the
next run will resume from where it left off without duplicating completed actions.

---

## Filing a Good Bug Report

Before opening a new issue, please check existing
[GitHub Issues](https://github.com/DotDev262/WinHome/issues) and
[Discussions](https://github.com/DotDev262/WinHome/discussions) to avoid duplicates.

When opening an issue, please include:

1. **WinHome version** — Check the release page for the version you downloaded.
2. **Windows version** — Run `winver` in PowerShell to get your exact Windows build.
3. **Full debug log** — Run with `--debug` and paste the complete output.
4. **Your config.yaml** — Remove any sensitive values before sharing.
5. **Steps to reproduce** — Describe exactly what you did and what you expected to happen.

Open a new issue here: [GitHub Issues](https://github.com/DotDev262/WinHome/issues)
