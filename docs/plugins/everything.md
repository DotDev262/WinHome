# Everything Plugin

## Overview

The Everything plugin manages configuration settings in the `Everything.ini` configuration file for the Everything desktop file search utility.

## Prerequisites

- Everything installed and settings directory present at `%APPDATA%\Everything`

## Configuration Schema

Settings are structured as objects grouped by INI section under the `settings` key:

- Section names (e.g. `[Everything]`) are top-level keys.
- Configuration key-value pairs are sub-keys under each section.
- Option keys case is preserved, and values are lowercased when written.

## Usage Examples

```yaml
plugins:
  - name: everything
    settings:
      Everything:
        run_in_background: true
        show_tray_icon: true
        match_path: false
```

## Verification Steps

To verify settings, check the contents of your Everything configuration file:
```bash
cat %APPDATA%\Everything\Everything.ini
```

## Notes / Caveats

- The plugin automatically creates the `%APPDATA%\Everything` directory if it does not exist when applying configuration.
