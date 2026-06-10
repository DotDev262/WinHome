# Spicetify Plugin

## Overview

The Spicetify plugin manages configuration settings in `config.ini` for the Spicetify customizer tool for Spotify.

## Prerequisites

- Spicetify installed (executable available in system PATH or settings folder present at `%USERPROFILE%\.spicetify`)

## Configuration Schema

Settings are structured as objects grouped by INI section under the `settings` key:

- Section names (e.g. `[Setting]`, `[AdditionalOptions]`) are top-level keys.
- Configuration key-value pairs are sub-keys under each section.
- Value normalization is applied:
  - Boolean values become `"1"` (True) or `"0"` (False).
  - Lists and tuples are joined into comma-separated lists (e.g. `[a, b]` -> `"a,b"`).
  - `None` value is converted to empty string `""`.

## Usage Examples

```yaml
plugins:
  - name: spicetify
    settings:
      Setting:
        current_theme: "dribbblish"
        color_scheme: "base"
        inject_css: true
```

## Verification Steps

To verify the installation:
```bash
spicetify --version
```
To verify settings, examine the spicetify configuration file:
```bash
cat ~/.spicetify/config.ini
```

## Notes / Caveats

- All settings are modified persistently in `%USERPROFILE%\.spicetify\config.ini`.
- If the file is corrupt or unparsable, a backup of the corrupted file is created, and a new parser is initialized.
