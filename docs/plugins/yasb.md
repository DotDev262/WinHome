# YASB Plugin

## Overview

The YASB (Yet Another Status Bar) plugin manages configuration settings in `config.yaml` for YASB.

## Prerequisites

- YASB installed (`yasb.exe` or `yasb` in PATH or configuration folder exists at `%USERPROFILE%\.config\yasb`)

## Configuration Schema

The plugin recursively deep merges the dictionary specified under the `settings` key directly into YASB's `config.yaml` file.

## Usage Examples

```yaml
plugins:
  - name: yasb
    settings:
      watch_config: true
      bars:
        main_bar:
          enabled: true
          screens: ["*"]
```

## Verification Steps

To verify settings, inspect YASB's configuration file:
```bash
cat ~/.config/yasb/config.yaml
```

## Notes / Caveats

- All configurations are written to `%USERPROFILE%\.config\yasb\config.yaml`.
- In case of file corruption or parsing errors, a backup is generated (`*.bak`), and a fresh settings dictionary is initialized.
