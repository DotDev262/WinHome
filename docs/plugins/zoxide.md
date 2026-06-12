# Zoxide Plugin

## Overview

The Zoxide plugin configures smart directory navigation using `zoxide`.

## Prerequisites

- zoxide installed
- Shell support (PowerShell / Bash)

## Configuration Schema

| Key       | Type   | Description           |
| --------- | ------ | --------------------- |
| init.cmd  | string | Alias command         |
| init.hook | string | Hook type             |
| env_vars  | object | Environment variables |

## Usage Examples

### Basic setup

```yaml
plugins:
  - name: zoxide
    init: {}
```

### Custom alias

```yaml
plugins:
  - name: zoxide
    init:
      cmd: 'z'
```

## Verification Steps

```bash
zoxide --version
```

## Notes / Caveats

- Updates shell profiles automatically
- Works across PowerShell and Bash

```

```
