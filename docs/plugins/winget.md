# winget plugin

## Description

The `winget` plugin manages configuration for Windows Package Manager (winget). It writes settings
to winget's `settings.json` file, allowing you to declaratively control winget's behaviour as part
of your WinHome setup.

## Prerequisites

- winget must be installed (comes pre-installed on Windows 11; available via the
  [App Installer](https://apps.microsoft.com/store/detail/app-installer/9NBLGGH4NNS1) on Windows 10)
- winget is detected via `winget.exe` or `winget`
- The `LOCALAPPDATA` environment variable must be set (standard on Windows)
- **Windows only**

## Configuration file location

| Platform | Path                                                                                           |
| -------- | ---------------------------------------------------------------------------------------------- |
| Windows  | `%LOCALAPPDATA%\Packages\Microsoft.DesktopAppInstaller_8wekyb3d8bbwe\LocalState\settings.json` |

## Configuration format

```yaml
plugins:
  winget:
    settings:
      <section>:
        <key>: <value>
```

Settings are deep-merged into winget's `settings.json`. Nested objects map to JSON sections in
winget's settings schema.

## Supported settings

Any valid winget settings option is supported. Common sections include:

| Section           | Key                           | Type    | Description                                       |
| ----------------- | ----------------------------- | ------- | ------------------------------------------------- |
| `visual`          | `progressBar`                 | string  | Progress bar style (`accent`, `rainbow`, `retro`) |
| `installBehavior` | `preferences`                 | object  | Preferred installer scope/type                    |
| `installBehavior` | `requirements`                | object  | Required installer scope/type                     |
| `source`          | `autoUpdateIntervalInMinutes` | integer | Source update interval (0 to disable)             |
| `telemetry`       | `disable`                     | boolean | Disable telemetry                                 |
| `network`         | `downloader`                  | string  | Download handler (`default`, `wininet`, `do`)     |
| `logging`         | `level`                       | string  | Log level (`verbose`, `info`, `warning`, `error`) |

## Usage examples

### Example 1 — Disable telemetry and set a rainbow progress bar

```yaml
plugins:
  winget:
    settings:
      telemetry:
        disable: true
      visual:
        progressBar: rainbow
```

### Example 2 — Configure install behaviour and disable auto source updates

```yaml
plugins:
  winget:
    settings:
      installBehavior:
        preferences:
          scope: machine
      source:
        autoUpdateIntervalInMinutes: 0
```

### Example 3 — Set verbose logging and use WinINet as the downloader

```yaml
plugins:
  winget:
    settings:
      logging:
        level: verbose
      network:
        downloader: wininet
```

## Notes

- Settings are deep-merged — existing winget settings not mentioned in `config.yaml` are preserved.
- The plugin handles the UTF-8 BOM that winget sometimes adds to `settings.json`.
- Supports `dryRun` mode — logs what would change without writing to disk.
- **Windows only** — this plugin has no effect on other operating systems.
- The `LOCALAPPDATA` environment variable must be set; the plugin will error if it is missing.

## Verification

After applying, open winget settings to confirm:

```bash
winget settings
```

Or inspect the file directly:

```bash
type "%LOCALAPPDATA%\Packages\Microsoft.DesktopAppInstaller_8wekyb3d8bbwe\LocalState\settings.json"
```
