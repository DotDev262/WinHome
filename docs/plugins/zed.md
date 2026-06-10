# Zed Plugin

## Overview

The Zed plugin manages JSONC configuration settings in the `settings.json` file for the Zed editor.

## Prerequisites

- Zed editor installed (`zed.exe` or `zed` in PATH)

## Configuration Schema

Settings accept key-value pairs under the `settings` key (or root-level parameters inside the plugin configuration):

- Strips JSONC comments from the file before parsing.
- String values are normalized to their expected types for common settings:
  - Boolean keys (e.g. `vim_mode`, `relative_line_numbers`, `copilot`) or strings matching `"true"`/`"false"` are converted to actual booleans.
  - Integer keys (e.g. `font_size`, `buffer_font_size`, `tab_size`) are converted to integers.

## Usage Examples

```yaml
plugins:
  - name: zed
    settings:
      theme: "One Dark"
      vim_mode: true
      font_size: 14
      tab_size: 4
```

## Verification Steps

To verify the updated settings, check the contents of your Zed configuration file:
```bash
cat %APPDATA%\Zed\settings.json
```

## Notes / Caveats

- All configurations are written to `%APPDATA%\Zed\settings.json` by default. An explicit configuration path can be specified via `args.configPath` / `args.config_path` or `context.configPath` / `context.config_path`.
- If a settings file is corrupted or failed to parse, the plugin automatically creates a backup copy (`*.bak`) and initializes a clean configuration.
