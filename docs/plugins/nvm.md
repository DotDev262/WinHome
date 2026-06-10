# NVM Plugin

## Overview

The NVM plugin manages settings for the Node Version Manager (`nvm-windows`).

## Prerequisites

- Node Version Manager for Windows installed (`nvm.exe` in PATH or settings file existing)

## Configuration Schema

Configuration is managed via `%APPDATA%\nvm\settings.txt`. Common keys:

| Key | Description |
|-----|-------------|
| root | Path to the NVM directory |
| path | Path to the Node.js symlink |
| arch | Target Node.js architecture (e.g. `64`, `32`) |
| proxy | Proxy URL for downloads (or `none`) |

## Usage Examples

```yaml
plugins:
  - name: nvm
    settings:
      root: "C:\\Users\\Username\\AppData\\Roaming\\nvm"
      path: "C:\\Program Files\\nodejs"
      arch: 64
      proxy: "none"
```

## Verification Steps

```bash
nvm version
```

## Notes / Caveats

- This plugin specifically targets **NVM for Windows** (`nvm-windows`).
- Other settings and comments in the `settings.txt` file are preserved during modifications.
