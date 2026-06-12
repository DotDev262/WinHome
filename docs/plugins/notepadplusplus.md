# Notepad++ Plugin

## Overview

The Notepad++ plugin manages Notepad++ configuration settings.

It can:

- Create or update the Notepad++ configuration file
- Apply custom settings
- Merge new settings with existing configuration
- Preserve existing values that are not modified

## Prerequisites

- Notepad++ installed
- Windows environment with `%APPDATA%` configured
- Permission to write to `%APPDATA%\Notepad++`

## Configuration Schema

| Field    | Type   | Default | Description                                           |
| -------- | ------ | ------- | ----------------------------------------------------- |
| settings | object | none    | Key-value settings written to Notepad++ configuration |

### Settings Example

```yaml
settings:
  theme: DarkMode
  fontSize: 14
```

## Usage Examples

### Basic Configuration

```yaml
settings:
  theme: DarkMode
```

### Multiple Settings

```yaml
settings:
  theme: DarkMode
  fontSize: 14
  autoSave: true
```

### Editor Preferences

```yaml
settings:
  wordWrap: true
  showLineNumbers: true
  autoIndent: true
```

## Verification Steps

1. Apply the configuration.
2. Open `%APPDATA%\Notepad++\config.json`.
3. Verify the configured values exist.
4. Launch Notepad++.
5. Confirm the settings are reflected in the application.

## Notes / Caveats

- Existing settings are merged rather than replaced.
- Unknown settings are written without validation.
- The plugin stores configuration in JSON format.
- The `%APPDATA%` environment variable must exist.
- Invalid values may not be recognized by Notepad++.
