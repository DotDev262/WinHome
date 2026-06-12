# Yazi Plugin

## Overview

The Yazi plugin manages configuration for the Yazi terminal file manager using TOML files.

## Prerequisites

- Yazi installed
- Windows AppData access

## Configuration Schema

| File        | Purpose        |
| ----------- | -------------- |
| yazi.toml   | Main config    |
| keymap.toml | Key bindings   |
| theme.toml  | Theme settings |

## Usage Examples

### Manager config

```yaml
plugins:
  - name: yazi
    manager:
      show_hidden: true
```

### Keymap config

```yaml
plugins:
  - name: yazi
    keymap:
      manager: []
```

## Verification Steps

```bash
yazi --version
```

## Notes / Caveats

- TOML merging supported
- Unknown keys ignored safely

```

```
