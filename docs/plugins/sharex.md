# ShareX Plugin

## Overview

This plugin manages ShareX's JSON configuration file at `%APPDATA%\ShareX\ShareX.json`. It performs a recursive merge so you can update only the parts you care about without rewriting the whole file.

## Prerequisites

- Windows only.
- ShareX installed.
- `APPDATA` must be set.
- The current user must have write access to the ShareX profile folder.

## Configuration Schema

The plugin accepts a top-level YAML object with a single supported field:

| Field | Type | Default | Description |
| --- | --- | --- | --- |
| `settings` | object | none | Recursively merged into `ShareX.json`. Any nested object shape is accepted. |

The plugin does not enforce a ShareX-specific schema. It simply deep-merges the JSON object tree you provide.

### Merge behavior

- Objects are merged recursively.
- Non-object values replace the existing value.
- New keys are added.
- If the current file is missing, the plugin starts from an empty object.
- If the file is corrupted JSON, the plugin backs it up to a timestamped `.corrupted` file and starts fresh.

## Usage Examples

### Minimal update

```yaml
settings:
  Application:
    StartWithWindows: true
```

### Nested settings update

```yaml
settings:
  TaskSettings:
    AfterCaptureTask:
      OpenInImageEditor: true
    AfterUploadTask:
      CopyURLToClipboard: true
```

### Multi-area update

```yaml
settings:
  Application:
    StartMinimized: true
  HotkeySettings:
    CaptureRegion: "Ctrl+Shift+R"
  History:
    MaxItemCount: 100
```

## Verification Steps

1. Confirm ShareX is installed and `%APPDATA%\ShareX` exists.
2. Apply your configuration.
3. Open `%APPDATA%\ShareX\ShareX.json`.
4. Verify the expected nested keys were added or updated.
5. Launch ShareX and confirm the setting appears in the UI if the option is exposed there.

## Notes / Caveats

- The plugin never validates whether a key is a real ShareX option.
- Corrupted JSON is backed up automatically before the file is replaced.
- Because merging is recursive, providing a partial object only changes the matching subtree.
- Dry runs are controlled by the execution context, not by a YAML field.
