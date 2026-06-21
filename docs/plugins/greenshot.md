# Greenshot Plugin

## Overview

The Greenshot plugin manages the configuration of the Greenshot screenshot utility by deep-merging settings into `%APPDATA%\Greenshot\Greenshot.ini`.

## Prerequisites

- Windows operating system with `%APPDATA%` available
- Greenshot installed (or a writable `%APPDATA%\Greenshot` directory)

## Configuration Schema

The plugin accepts a top-level YAML object with a single configuration field:

| Field | Type | Default | Description |
| ----- | ---- | ------- | ----------- |
| `settings` | object | none | Key-value pairs to write to `Greenshot.ini`. |

### Key Mapping Format

Because Greenshot's configuration uses the INI format with multiple sections, keys in `settings` must map to their respective sections in `Greenshot.ini` using a backslash (`\`) as a separator:

- **Section Keys**: Format the key as `"Section\\Key"` (or `'Section\Key'` in single quotes) to target a specific section.
- **Default Section**: If no backslash is present in the key, it defaults to the `[General]` section.

### Common Configuration Sections and Keys

While any settings accepted by Greenshot can be written, here are some commonly used sections and keys:

| Compound Key | Type | Description |
| ------------ | ---- | ----------- |
| `General\Language` | string | Interface language code (e.g., `"en-US"`). |
| `Capture\CaptureMousepointer` | boolean | If `true`, the cursor is included in screenshots. |
| `Capture\CaptureMode` | string | The capture mode (e.g., `"Region"`, `"Window"`). |
| `Destination\CopyToClipboard` | boolean | If `true`, screenshots are automatically copied to the clipboard. |
| `Core\OutputFilePath` | string | The directory path where screenshots are saved. |
| `Core\OutputFormat` | string | Screenshot file format (e.g., `"png"`, `"jpg"`, `"gif"`). |

## Usage Examples

### Configure file output options

Save screenshots directly to a custom path in PNG format:

```yaml
extensions:
  greenshot:
    settings:
      "Core\\OutputFilePath": "C:\\Users\\Username\\Pictures\\Screenshots"
      "Core\\OutputFormat": "png"
```

### Enable clipboard destination and capture cursor

Include the mouse cursor and automatically copy captured screenshots to the clipboard:

```yaml
extensions:
  greenshot:
    settings:
      "Capture\\CaptureMousepointer": true
      "Destination\\CopyToClipboard": true
```

### Combined capture and interface configuration

Configure the display language, capture options, and output settings in one block:

```yaml
extensions:
  greenshot:
    settings:
      "General\\Language": "en-US"
      "Capture\\CaptureMousepointer": false
      "Destination\\CopyToClipboard": true
      "Core\\OutputFilePath": "D:\\Screenshots"
      "Core\\OutputFormat": "png"
```

## Verification Steps

1. Apply your WinHome configuration.
2. Verify that `%APPDATA%\Greenshot\Greenshot.ini` has been updated with the specified sections and values. You can run:
   ```powershell
   Get-Content "$env:APPDATA\Greenshot\Greenshot.ini"
   ```
3. Open the Greenshot settings panel from the system tray and confirm the changes are reflected.

## Notes / Caveats

- This plugin is Windows-specific as it targets the AppData directory structure.
- Values are written as strings (`"True"`/`"False"` for booleans, and standard string representation for others).
- Existing configurations in `Greenshot.ini` are preserved unless explicitly overwritten by the settings provided.
- The configuration file is updated atomically via a temporary file write followed by a replace operation to prevent corruption.
- If Greenshot is currently running, it is recommended to close the application before applying settings, or restart it afterward for the changes to take effect.
- Supports `dryRun` mode to check for changes without modifying the filesystem.
