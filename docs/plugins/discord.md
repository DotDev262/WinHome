# Discord Plugin

## Overview

The Discord plugin manages configuration settings in `settings.json` for the Discord desktop chat client.

## Prerequisites

- Discord installed and configuration folder present at `%APPDATA%\discord`

## Configuration Schema

The plugin recursively deep merges the dictionary specified under the `settings` key directly into Discord's configuration file.

## Usage Examples

```yaml
plugins:
  - name: discord
    settings:
      IS_MAXIMIZED: true
      IS_MINIMIZED: false
      WINDOW_BOUNDS:
        width: 1280
        height: 720
```

## Verification Steps

To verify settings, inspect the configuration file:
```bash
cat %APPDATA%\discord\settings.json
```

## Notes / Caveats

- All configurations are written to `%APPDATA%\discord\settings.json`.
- The plugin automatically creates the `%APPDATA%\discord` directory if it does not exist when applying configuration.
