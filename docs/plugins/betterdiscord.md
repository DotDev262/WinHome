# BetterDiscord Plugin

## Overview

The BetterDiscord plugin manages configuration settings in `settings.json` for BetterDiscord, the custom client customization platform for Discord.

## Prerequisites

- BetterDiscord settings directory present at `%APPDATA%\BetterDiscord`

## Configuration Schema

The plugin recursively deep merges the dictionary specified under the `settings` key directly into BetterDiscord's configuration file.

## Usage Examples

```yaml
plugins:
  - name: betterdiscord
    settings:
      settings:
        general:
          voiceDisconnect: true
      window:
        width: 1280
        height: 720
```

## Verification Steps

To verify the updated configuration settings, check the JSON file:
```bash
cat %APPDATA%\BetterDiscord\data\settings.json
```

## Notes / Caveats

- Settings are written to `%APPDATA%\BetterDiscord\data\settings.json`.
- A backup copy of the config is automatically generated in case of file corruptions or parsing failures.
