# Syncthing Plugin

## Overview

The Syncthing plugin manages XML configuration settings in `config.xml` for the Syncthing file synchronization tool.

## Prerequisites

- Syncthing installed (`syncthing.exe` in PATH)

## Configuration Schema

Settings accept structured configurations under the `settings` key. Mapped sections:

| Key | Section | Description |
|-----|---------|-------------|
| `gui` | `<gui>` | Configuration parameters for the web GUI interface. |
| `options` | `<options>` | System-wide options configuration. |

- Boolean values are automatically cast to lowercase strings (`"true"` or `"false"`).

## Usage Examples

```yaml
plugins:
  - name: syncthing
    settings:
      gui:
        enabled: true
        address: "127.0.0.1:8384"
      options:
        urAccepted: -1
```

## Verification Steps

To verify the updated configuration settings, check the XML file:
```bash
cat %LOCALAPPDATA%\Syncthing\config.xml
```

## Notes / Caveats

- Syncthing configuration is saved to `%LOCALAPPDATA%\Syncthing\config.xml` on Windows, and `~/.config/syncthing/config.xml` on other systems.
- If the configuration file does not exist, a default root structure with `version="37"` is generated.
