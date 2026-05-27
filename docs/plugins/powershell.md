# PowerShell Plugin

## Description

The PowerShell plugin manages PowerShell profile configuration by updating the user's PowerShell profile script.

The plugin can:
- Configure aliases
- Import PowerShell modules
- Configure prompts such as Oh My Posh
- Configure PSReadLine options
- Add custom PowerShell functions
- Preserve existing profile content outside managed sections

Managed configuration blocks are automatically wrapped between markers:

```powershell
# --- WinHome managed start ---
# --- WinHome managed end ---
```

## Supported OS

- Windows
- Linux
- macOS

## PowerShell Profile Locations

### Windows PowerShell 7

```text
%USERPROFILE%\Documents\PowerShell\Microsoft.PowerShell_profile.ps1
```

### Windows PowerShell 5

```text
%USERPROFILE%\Documents\WindowsPowerShell\Microsoft.PowerShell_profile.ps1
```

### Linux/macOS

```text
~/.config/powershell/profile.ps1
```

## Configuration

Basic example:

```yaml
plugins:
  - name: powershell
    settings:
      aliases:
        ll: "Get-ChildItem"
```

## Supported Settings

| Setting | Description |
|----------|-------------|
| aliases | Configure PowerShell aliases |
| modules | Import PowerShell modules |
| prompt | Configure shell prompt |
| psreadline | Configure PSReadLine options |
| functions | Add custom PowerShell functions |

---

## Usage Examples

### Example 1 — Configure Aliases

```yaml
plugins:
  - name: powershell
    settings:
      aliases:
        ll: "Get-ChildItem"
        gs: "git status"
```

### Example 2 — Configure Oh My Posh Prompt

```yaml
plugins:
  - name: powershell
    settings:
      prompt:
        type: "oh-my-posh"
        theme: "~/themes/jandedobbeleer.omp.json"
```

### Example 3 — Configure PSReadLine

```yaml
plugins:
  - name: powershell
    settings:
      psreadline:
        prediction_source: "History"
```

### Example 4 — Import Modules

```yaml
plugins:
  - name: powershell
    settings:
      modules:
        zoxide: {}
```

### Example 5 — Add Custom Functions

```yaml
plugins:
  - name: powershell
    settings:
      functions:
        greet: |
          Write-Host "Hello from WinHome"
```

---

## Verification

Verify PowerShell installation:

```powershell
$PSVersionTable
```

Verify aliases:

```powershell
Get-Alias ll
```

Verify imported modules:

```powershell
Get-Module
```

Verify profile location:

```powershell
$PROFILE
```

---

## Notes

- Existing profile content outside WinHome managed markers is preserved.
- The plugin automatically creates profile directories if they do not exist.
- The plugin supports dry-run mode.
- PowerShell modules are imported with `ErrorAction SilentlyContinue`.
- Managed sections are automatically updated without overwriting unrelated profile content.