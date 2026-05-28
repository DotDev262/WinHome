# Rclone Plugin

## Overview

This plugin manages the `rclone.conf` file in the current user's profile. It preserves existing INI formatting as much as possible while merging global settings and named remote sections.

## Prerequisites

- Windows only.
- `USERPROFILE` must be set.
- Rclone must already be installed if you want `check_installed` to return true.
- The user must have permission to write to `%USERPROFILE%\.config\rclone\rclone.conf`.

## Configuration Schema

The plugin accepts a top-level YAML object with these fields:

| Field | Type | Default | Description |
| --- | --- | --- | --- |
| `settings` | object | none | Merges key/value pairs into the global section of `rclone.conf` before the first INI section. |
| `remotes` | object | none | Map of remote names to remote settings objects. Each key becomes an INI section like `[remote-name]`. |

The plugin does not validate rclone backend options. It simply writes the keys and values you provide.

### Global settings

`settings` is written into the global section. Values are stringified when written, so use simple scalar values.

### Remote settings

Each entry under `remotes` is written into a section matching the remote name.

| Field | Type | Default | Description |
| --- | --- | --- | --- |
| `remotes.<name>` | object | none | Key/value pairs for the remote section. Existing keys are updated in place. New keys are appended. |

## Usage Examples

### Minimal remote definition

```yaml
remotes:
  gdrive:
    type: drive
    scope: drive
    root_folder_id: ""
```

### Global settings plus one remote

```yaml
settings:
  checkers: 8
  transfers: 4
remotes:
  backup:
    type: s3
    provider: AWS
    env_auth: true
```

### Multiple remotes

```yaml
remotes:
  work:
    type: onedrive
    token: '{"access_token":"..."}'
  media:
    type: local
    nounc: true
```

## Verification Steps

1. Confirm `rclone.exe` is installed or available on `PATH`.
2. Apply your configuration.
3. Open `%USERPROFILE%\.config\rclone\rclone.conf`.
4. Verify the global section and remote sections contain the merged keys you expected.
5. Run a normal rclone command such as `rclone listremotes` or `rclone config file` to confirm rclone can read the file.

## Notes / Caveats

- The plugin preserves comments, blank lines, and existing section order when possible.
- New keys are appended to the end of the matching section.
- If the config file uses CRLF, the plugin keeps CRLF on write.
- Values are written as strings, so avoid nested objects for rclone settings.
- Dry runs are controlled by the execution context, not by a YAML field.
