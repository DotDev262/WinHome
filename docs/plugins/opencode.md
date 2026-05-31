# opencode plugin

## Description

The `opencode` plugin manages OpenCode configuration on Windows. It deep-merges JSON settings into `opencode.json`, supporting standard JSON and JSONC source files with comments stripped on read.

## Prerequisites

- OpenCode CLI must be installed and available as `opencode` on `PATH`
- Write access to the target config directory

## Configuration file location

| Condition | Path |
|-----------|------|
| Default (user) | `%USERPROFILE%\.config\opencode\opencode.json` |
| Project scope | `<projectRoot>\opencode.json` when `projectRoot` is set |
| Explicit override | Path from `configPath` / `config_path` |

Resolution order: explicit `configPath` → `projectRoot` → user default.

## Configuration format

```yaml
extensions:
  opencode:
    settings:
      <key>: <value>
```

Alternatively, omit the `settings` wrapper and place config keys at the top level of the plugin block (path keys `projectRoot`, `project_root`, `configPath`, `config_path` are excluded from the merge).

### Project-scoped config

```yaml
extensions:
  opencode:
    projectRoot: D:\dev\my-app
    settings:
      model: gpt-4o
```

Writes to `D:\dev\my-app\opencode.json`.

## Supported settings

Any valid OpenCode JSON config key is supported. The plugin does not enforce a schema — refer to OpenCode documentation for available options.

| Field | Type | Description |
|-------|------|-------------|
| `settings` | object | Config object deep-merged into `opencode.json` |
| `projectRoot` | string | Use project-local `opencode.json` |
| `configPath` | string | Explicit absolute or relative config file path |

## Usage examples

### User-level defaults

```yaml
extensions:
  opencode:
    settings:
      provider: openai
      temperature: 0.2
```

### Per-project configuration

```yaml
extensions:
  opencode:
    projectRoot: C:\Users\me\Projects\web-app
    settings:
      model: claude-sonnet-4
```

### Explicit config path

```yaml
extensions:
  opencode:
    configPath: C:\Users\me\.config\opencode\work.json
    settings:
      timeout: 120
```

## Notes

- Existing JSONC files are parsed with comments removed; writes output plain formatted JSON.
- Objects are deep-merged; non-object values replace existing keys.
- Supports `dryRun` mode — logs affected keys without writing.
- Config key in WinHome: `extensions.opencode`.

## Verification

After applying, inspect the resolved config file:

```powershell
Get-Content "$env:USERPROFILE\.config\opencode\opencode.json"
```

Run OpenCode with your usual workflow and confirm the merged settings take effect.
