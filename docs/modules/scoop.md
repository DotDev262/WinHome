# Scoop

Installs packages using Scoop. Scoop is a command-line package manager for Windows that focuses on
developer tools and open-source software. It installs programs cleanly into your user directory
without needing admin privileges.

**YAML Key:** `scoop`

**Properties:**

- `id`: The package identifier (e.g., `nodejs`).

---

## Basic Usage

Scoop installs packages from "buckets" (repositories of package definitions). You can search for
packages at [scoop.sh](https://scoop.sh) or by running:

```powershell
scoop search <app-name>
```

To install a single package using WinHome:

```yaml
scoop:
  - id: nodejs
```

To install multiple packages at once:

```yaml
scoop:
  - id: nodejs
  - id: python
  - id: git
```

---

## Advanced Configuration

### Adding Buckets

Scoop organizes packages into buckets. The default bucket covers common tools, but many packages
live in extra buckets like `extras`, `games`, or `nerd-fonts`. You can add buckets manually before
running WinHome:

```powershell
scoop bucket add extras
scoop bucket add nerd-fonts
```

### Real-World config.yaml Examples

**Example 1 — Developer Tools Setup**

```yaml
scoop:
  - id: git
  - id: nodejs
  - id: python
  - id: curl
```

**Example 2 — Terminal & Shell Setup**

```yaml
scoop:
  - id: starship
  - id: neovim
  - id: windows-terminal
  - id: fzf
```

**Example 3 — Fonts & UI Extras**

```yaml
scoop:
  - id: FiraCode-NF
  - id: JetBrainsMono-NF
  - id: Cascadia-Code
```

> Note: Add the `nerd-fonts` bucket first: `scoop bucket add nerd-fonts`

---

## Troubleshooting

**Issue: `scoop` is not recognized as a command**

- Scoop is not pre-installed on Windows. Install it by running this in PowerShell:

```powershell
  Set-ExecutionPolicy RemoteSigned -Scope CurrentUser
  irm get.scoop.sh | iex
```

**Issue: Package not found**

- The package may be in a different bucket. Try adding the `extras` bucket:

```powershell
  scoop bucket add extras
```

- Then search again: `scoop search <app-name>`

**Issue: Installation fails due to permissions**

- Scoop is designed to work without admin rights. If you face issues, make sure you are NOT running
  PowerShell as Administrator, as this can cause path conflicts.

**Issue: Package installs but doesn't work correctly**

- Run `scoop checkup` to diagnose common configuration problems and follow the suggestions it
  provides.
