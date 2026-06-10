# Lazydocker Plugin

## Overview

The Lazydocker plugin manages YAML configurations for the `lazydocker` Docker terminal UI manager.

## Prerequisites

- lazydocker installed and available on system PATH (`lazydocker.exe` or `lazydocker`)

## Configuration Schema

The plugin recursively deep merges the dictionary specified under the `settings` key directly into the configuration YAML file.

## Usage Examples

```yaml
plugins:
  - name: lazydocker
    settings:
      gui:
        theme:
          activeBorderColor:
            - green
            - bold
      reporting: "off"
```

## Verification Steps

To verify the updated settings, check the contents of your lazydocker configuration file:
```bash
cat %APPDATA%\lazydocker\config.yml
```

## Notes / Caveats

- All configurations are written to `%APPDATA%\lazydocker\config.yml`.
- If the configuration file exists but fails to parse, a backup is automatically created (`*.bak`) and a fresh file is written.
