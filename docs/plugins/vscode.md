# VS Code Plugin

## Overview

The VS Code plugin manages Visual Studio Code configuration, extensions, and user profiles.

It can:

- Install VS Code extensions
- Update VS Code settings
- Create and manage named profiles
- Apply settings to specific profiles

## Prerequisites

- Visual Studio Code installed
- `code` CLI command available in PATH
- Windows environment with `%APPDATA%` configured
- Permission to modify VS Code user settings

## Configuration Schema

| Field      | Type   | Default | Description                                     |
| ---------- | ------ | ------- | ----------------------------------------------- |
| settings   | object | none    | VS Code settings written to settings.json       |
| extensions | array  | none    | List of VS Code extension IDs to install        |
| profiles   | object | none    | Named VS Code profiles and their configurations |

### Settings Example

```yaml
settings:
  editor.fontSize: 16
  editor.wordWrap: 'on'
```

### Extensions Example

```yaml
extensions:
  - ms-python.python
  - esbenp.prettier-vscode
```

## Usage Examples

### Basic Settings

```yaml
settings:
  editor.fontSize: 16
```

### Install Extensions

```yaml
extensions:
  - ms-python.python
  - ms-vscode.cpptools
```

### Named Profile

```yaml
profiles:
  Development:
    extensions:
      - ms-python.python

    settings:
      editor.fontSize: 16
      editor.wordWrap: 'on'
```

## Verification Steps

1. Apply the configuration.
2. Open VS Code.
3. Verify extensions are installed.
4. Open Settings and confirm changes were applied.
5. Switch profiles if configured and verify profile-specific settings.

## Notes / Caveats

- Existing settings are merged rather than replaced.
- Extensions are installed using the VS Code CLI.
- The plugin requires the `code` command to be available.
- Profile creation depends on VS Code profile storage.4
- Named profiles are automatically created if they do not already exist.
- Settings are merged with existing settings.json values.
- Extension names should be valid VS Code extension IDs.
