# PowerShell Plugin

## Overview

The PowerShell plugin modifies the PowerShell profile to inject aliases, modules, and functions.

## Prerequisites

- PowerShell installed (`pwsh` or `powershell.exe`)
- Profile file access

## Configuration Schema

| Key       | Type   | Description        |
| --------- | ------ | ------------------ |
| aliases   | object | Command aliases    |
| modules   | object | PowerShell modules |
| functions | object | Custom functions   |
| prompt    | object | Prompt config      |

## Usage Examples

### Aliases

```yaml
plugins:
  - name: powershell
    aliases:
      ll: 'Get-ChildItem'
```

### Modules

```yaml
plugins:
  - name: powershell
    modules:
      posh-git: {}
```

## Verification Steps

```powershell
$PROFILE
```

```powershell
Get-Alias
```

## Notes / Caveats

- Edits PowerShell profile directly
- Uses markers to avoid duplicate injection

```

```
