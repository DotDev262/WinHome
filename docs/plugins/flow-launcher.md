# Flow Launcher Plugin

## Overview

The Flow Launcher plugin manages settings for [Flow Launcher](https://www.flowlauncher.com/) — a
desktop search utility for Windows. It reads and writes directly to the Flow Launcher
`Settings.json` file, giving WinHome control over hotkeys, search options, plugin configuration,
and theme settings.

## Prerequisites

- Flow Launcher installed
- Windows only (uses `%APPDATA%`)

## Configuration Schema

| Key | Purpose |
| --- | ------- |
| Hotkey | Global activation shortcut |
| Theme | UI theme name |
| SearchPluginsEnabled | Enable/disable plugin search results |
| ColorScheme | Accent color scheme |
| AutoUpdates | Enable automatic updates |
| Plugins | Nested map of per-plugin settings |

The full set of supported keys is defined by Flow Launcher's `Settings.json` schema, which follows
a flat or nested key-value structure. The plugin deep-merges any keys provided under
`extensions.flow-launcher.settings` into the existing settings file.

## Usage Examples

### Basic hotkey and theme

```yaml
extensions:
  flow-launcher:
    settings:
      Hotkey: "Alt+Space"
      Theme: "Dark"
      ColorScheme: "System"
```

### Enable/disable plugin search

```yaml
extensions:
  flow-launcher:
    settings:
      SearchPluginsEnabled: false
      AutoUpdates: true
```

### Per-plugin settings

```yaml
extensions:
  flow-launcher:
    settings:
      Hotkey: "Ctrl+Alt+K"
      Theme: "Light"
      Plugins:
        Flow.Launcher.Plugin.Calculator:
          Precision: 4
        Flow.Launcher.Plugin.WebSearch:
          DefaultBrowser: true
```

## Verification Steps

1. Run WinHome apply and confirm no errors:

   ```bash
   winhome apply
   ```

2. Open Flow Launcher (default hotkey) and verify the settings took effect.

3. Inspect the settings file directly:

   ```bash
   type "%APPDATA%\FlowLauncher\Settings\Settings.json"
   ```

## Notes / Caveats

- Only Windows is supported (relies on `%APPDATA%`).
- The plugin uses atomic writes via `tempfile.mkstemp()` + `os.replace()` to prevent corruption.
- Plugins settings are deep-merged, so existing per-plugin configuration is preserved when only
  updating specific plugin keys.
