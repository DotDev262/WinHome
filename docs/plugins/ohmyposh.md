# Oh My Posh Plugin

## Description

The Oh My Posh plugin manages Oh My Posh initialization inside the PowerShell profile.

The plugin can:
- Configure Oh My Posh themes
- Automatically update PowerShell profile files
- Replace existing Oh My Posh managed blocks
- Detect whether Oh My Posh configuration is already installed

Managed blocks are wrapped between:

```powershell
# OH-MY-POSH-PLUGIN BEGIN
# OH-MY-POSH-PLUGIN END
```

## Supported OS

- Windows

## PowerShell Profile Locations

### PowerShell 7

```text
%USERPROFILE%\Documents\PowerShell\Microsoft.PowerShell_profile.ps1
```

### Windows PowerShell 5

```text
%USERPROFILE%\Documents\WindowsPowerShell\Microsoft.PowerShell_profile.ps1
```

The plugin automatically selects the available profile path.

## Configuration

Basic configuration example:

```yaml
plugins:
  - name: ohmyposh
    settings:
      theme: "~/themes/jandedobbeleer.omp.json"
```

## Supported Settings

| Setting | Description |
|----------|-------------|
| theme | Path to Oh My Posh theme file |
| profile | Optional custom PowerShell profile path |

---

## Usage Examples

### Example 1 — Basic Theme Configuration

```yaml
plugins:
  - name: ohmyposh
    settings:
      theme: "~/themes/jandedobbeleer.omp.json"
```

### Example 2 — Custom Profile Path

```yaml
plugins:
  - name: ohmyposh
    settings:
      theme: "~/themes/paradox.omp.json"
      profile: "~/Documents/PowerShell/Microsoft.PowerShell_profile.ps1"
```

### Example 3 — Minimal Theme Setup

```yaml
plugins:
  - name: ohmyposh
    settings:
      theme: "~/themes/atomic.omp.json"
```

---

## Verification

Verify Oh My Posh installation:

```powershell
oh-my-posh version
```

Verify the profile contains the plugin block:

```powershell
Get-Content $PROFILE
```

Verify the theme is active:

```powershell
oh-my-posh init pwsh
```

---

## Notes

- Existing Oh My Posh blocks are automatically replaced during updates.
- The plugin preserves unrelated PowerShell profile content.
- Dry-run mode is supported.
- If no profile path is specified, the plugin automatically detects the correct PowerShell profile location.
- The plugin inserts initialization using:

```powershell
oh-my-posh init pwsh --config "<theme>" | Invoke-Expression
```