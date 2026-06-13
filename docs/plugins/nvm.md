# NVM Plugin

## Overview

The NVM plugin manages configuration for Node Version Manager (nvm-windows) via its `settings.txt` file.

## Prerequisites

- nvm-windows installed
- AppData or UserProfile directory access

## Configuration Schema

| File | Purpose |
| ---- | ------- |
| settings.txt | NVM root, paths, proxies |

Keys are standard nvm settings like `root`, `path`, `proxy`, `node_mirror`, and `npm_mirror`.

## Usage Examples

### NVM config

```yaml
extensions:
  nvm:
    settings:
      node_mirror: "https://npmmirror.com/mirrors/node/"
      npm_mirror: "https://npmmirror.com/mirrors/npm/"
```

## Verification Steps

```bash
nvm version
```

## Notes / Caveats

- Preserves existing comments and spacing in `settings.txt`.
- Performs atomic file writes to prevent configuration corruption.
