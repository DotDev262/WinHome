# PNPM Plugin

## Overview

The PNPM plugin manages settings for the PNPM package manager by updating the global `.npmrc` configuration file.

## Prerequisites

- PNPM installed (`pnpm` in PATH)

## Configuration Schema

Configuration is written to:
- Windows: `%USERPROFILE%\.npmrc`
- Linux/macOS: `~/.npmrc`

Supported settings include camelCase keys which map to standard `.npmrc` fields:

| Key | npmrc Key | Description |
|-----|-----------|-------------|
| storeDir | `store-dir` | Path to the pnpm content-addressable store |
| globalDir | `global-dir` | Path to store globally installed packages |
| globalBinDir | `global-bin-dir` | Path where global package executables are linked |
| nodeVersion | `node-version` | Force a specific Node.js version |
| packageManager | `package-manager` | Declare preferred package manager |
| autoInstallPeers | `auto-install-peers` | Automatically install peer dependencies |
| strictPeerDependencies | `strict-peer-dependencies` | Fail on peer dependency conflicts |
| shamefullyHoist | `shamefully-hoist` | Hoist dependencies to root node_modules |

Any other keys in settings are normalized and written directly as `key=value`.

## Usage Examples

```yaml
plugins:
  - name: pnpm
    settings:
      storeDir: "D:\\pnpm-store"
      globalBinDir: "C:\\pnpm\\bin"
      autoInstallPeers: true
```

## Verification Steps

```bash
pnpm --version
```
To verify settings, view your global `.npmrc` file:
```bash
cat ~/.npmrc
```

## Notes / Caveats

- A backup of the existing `.npmrc` file is automatically created before any writes.
