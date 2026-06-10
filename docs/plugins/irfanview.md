# IrfanView Plugin

## Overview

The IrfanView plugin manages configuration settings in `i_view*.ini` files for the IrfanView image viewer.

## Prerequisites

- IrfanView installed (`i_view32.exe` or `i_view64.exe` in PATH or settings directory present at `%APPDATA%\IrfanView`)

## Configuration Schema

Settings are structured as objects grouped by INI section under the `settings` key:

- Section names (e.g. `[Language]`, `[Open]`) are top-level keys.
- Configuration key-value pairs are sub-keys under each section.
- Boolean values are automatically cast to `"1"` (True) or `"0"` (False).

## Usage Examples

```yaml
plugins:
  - name: irfanview
    settings:
      Language:
        DLL: "ENGLISH"
      Open:
        SaveDir: "C:\\Users\\Username\\Pictures"
```

## Verification Steps

To inspect the configuration, examine the active IrfanView INI file:
```bash
cat %APPDATA%\IrfanView\i_view64.ini
```

## Notes / Caveats

- The plugin automatically scans `%APPDATA%\IrfanView` for the active INI file matching `i_view*.ini`. If none exists, it defaults to creating `i_view64.ini`.
- A corruption backup is automatically generated if the active INI file cannot be parsed.
