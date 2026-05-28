# Oh My Posh Plugin

## Overview

The Oh My Posh plugin configures terminal themes using Oh My Posh prompt engine.

## Prerequisites

- Oh My Posh installed
- PowerShell or supported shell

## Configuration Schema

| Key | Type | Description |
|-----|------|-------------|
| theme | string | Path to theme file |
| profile | string | Optional profile path |

## Usage Examples

### Basic theme
```yaml
plugins:
  - name: ohmyposh
    theme: "atomic.omp.json"
```

### Custom profile
```yaml
plugins:
  - name: ohmyposh
    profile: "custom.ps1"
```

## Verification Steps

```bash
oh-my-posh --version
```

## Notes / Caveats

- Only modifies prompt initialization block
- Safe overwrite using markers
```
