# Scoop Plugin

## Overview

The Scoop plugin manages configuration settings in `config.json` for the Scoop package manager.

## Prerequisites

- Scoop installed and available on system PATH (`scoop.exe` or `scoop`)

## Configuration Schema

The plugin recursively deep merges the dictionary specified under the `settings` key directly into Scoop's configuration file.

## Usage Examples

```yaml
plugins:
  - name: scoop
    settings:
      proxy: "127.0.0.1:1080"
      show_update_log: true
```

## Verification Steps

To verify settings, examine the Scoop configuration file:
```bash
cat ~/.config/scoop/config.json
```

## Notes / Caveats

- Scoop configuration is read from `$env:XDG_CONFIG_HOME\scoop\config.json` if set, otherwise from `%USERPROFILE%\.config\scoop\config.json`.
- A backup of the current config file is automatically created before any writes.
