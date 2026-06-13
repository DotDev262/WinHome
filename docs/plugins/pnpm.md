# PNPM Plugin

## Overview

The pnpm plugin manages configuration for the pnpm package manager using the user's `.npmrc` file.

## Prerequisites

- pnpm installed
- Available in PATH

## Configuration Schema

| File | Purpose |
| ---- | ------- |
| .npmrc | pnpm and npm settings |

The plugin automatically translates camelCase settings to dash-case (e.g., `storeDir` -> `store-dir`).

## Usage Examples

### pnpm config

```yaml
extensions:
  pnpm:
    settings:
      storeDir: "D:\\.pnpm-store"
      shamefullyHoist: true
      strictPeerDependencies: false
```

## Verification Steps

```bash
pnpm --version
```

## Notes / Caveats

- Modifies the global `.npmrc` file in the user's home directory.
- Backs up the existing configuration file before making changes.
