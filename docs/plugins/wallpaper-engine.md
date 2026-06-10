# Wallpaper Engine Plugin

## Overview

The Wallpaper Engine plugin manages JSON configuration settings in the `config.json` preferences file for Wallpaper Engine.

## Prerequisites

- Wallpaper Engine installed (under Steam installation directory, e.g. `%ProgramFiles(x86)%\Steam\steamapps\common\wallpaper_engine\config\config.json`)

## Configuration Schema

The plugin recursively deep merges the dictionary specified under the `settings` key directly into the configuration JSON file.

## Usage Examples

```yaml
plugins:
  - name: wallpaper-engine
    settings:
      general:
        ui:
          skin: "dark"
```

## Verification Steps

To verify the updated settings, check the contents of your Wallpaper Engine configuration file:
```bash
cat "%ProgramFiles(x86)%\Steam\steamapps\common\wallpaper_engine\config\config.json"
```

## Notes / Caveats

- The plugin searches in typical Steam paths across both 32-bit and 64-bit Program Files directories.
- If the target configuration directory exists but the `config.json` file does not, the plugin will create a new file containing the merged settings.
