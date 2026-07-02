# Ripgrep Plugin

## Overview

The Ripgrep plugin manages configuration for `ripgrep` (rg) using the `.ripgreprc` config file.

## Prerequisites

- Ripgrep installed
- Available in PATH

## Configuration Schema

| File | Purpose |
| ---- | ------- |
| .ripgreprc | Ripgrep settings |

Any valid ripgrep command-line flag can be provided as a key (without the leading `--`).

## Usage Examples

### Ripgrep config

```yaml
extensions:
  ripgrep:
    settings:
      smart-case: true
      hidden: true
      max-columns: 150
```

## Verification Steps

```bash
rg --version
```

## Notes / Caveats

- Settings are written as `--key=value` or `--key` if boolean `true`.
- Safely handles corrupt configuration files by backing them up and starting fresh.
