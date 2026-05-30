# VS Code Plugin

## Overview

Syncs Visual Studio Code user settings, named profiles, and extension installation under `%APPDATA%\Code\User`. Supports the default profile and per-profile `settings.json` + extensions.

## Prerequisites

- Windows (uses `%APPDATA%`)
- VS Code installed with the `code` CLI on `PATH` for extension install/check commands
- Write access to `%APPDATA%\Code\User`

## Configuration Schema

Config key: `extensions.vscode` or top-level `vscode`.

| Field | Type | Description |
| --- | --- | --- |
| `settings` | object | Key/value pairs merged into the default profile `settings.json`. |
| `extensions` | string[] | Extension IDs installed via `code --install-extension`. |
| `profiles` | object | Map of profile name → `{ settings?, extensions? }`. Creates missing named profiles. |

Also supports `apps:` entries with `manager: vscode` and `packageId` as the extension ID (package_manager capability).

## Usage Examples

### Default profile settings + extensions

```yaml
vscode:
  settings:
    editor.tabSize: 2
    editor.formatOnSave: true
  extensions:
    - esbenp.prettier-vscode
    - ms-python.python
```

### Named work profile

```yaml
extensions:
  vscode:
    profiles:
      Work:
        settings:
          workbench.colorTheme: Default Dark Modern
        extensions:
          - GitHub.copilot
```

### Extensions-only via apps

```yaml
apps:
  - id: esbenp.prettier-vscode
    manager: vscode
```

## Verification Steps

1. Confirm `code --version` works in a shell.
2. Apply config; check `%APPDATA%\Code\User\settings.json` (or `profiles/<id>/settings.json`).
3. Run `code --list-extensions` and verify listed extensions appear.
4. Open VS Code and confirm settings/themes apply.

## Notes / Caveats

- Named profiles require an existing or newly created entry in `globalStorage/storage.json`.
- Settings merge is shallow per key (full value replacement when JSON differs).
- Extension install requires network and the VS Code CLI.
