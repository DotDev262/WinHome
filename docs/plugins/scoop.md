# scoop plugin

## Description

The `scoop` plugin manages Scoop client configuration on Windows. It deep-merges settings into Scoop's `config.json`, allowing you to declaratively control install paths, download behavior, and other Scoop options as part of your WinHome setup.

This plugin manages Scoop **configuration** only. Use the top-level `scoop:` block in `config.yaml` (or `apps:` entries with `manager: scoop`) for package installation workflows. See [Scoop module docs](../modules/scoop.md) for app installation.

## Prerequisites

- Scoop must be installed (see [scoop.sh](https://scoop.sh))
- Scoop is detected via `scoop.exe`, `scoop.cmd`, `scoop.ps1`, or `scoop` on `PATH`
- `USERPROFILE` must be set (standard on Windows), or `XDG_CONFIG_HOME` when using a custom config root

## Configuration file location

| Condition | Path |
|-----------|------|
| `XDG_CONFIG_HOME` is set | `%XDG_CONFIG_HOME%\scoop\config.json` |
| Default | `%USERPROFILE%\.config\scoop\config.json` |

This matches Scoop's own config resolution in `lib/core.ps1`.

## Configuration format

```yaml
extensions:
  scoop:
    settings:
      <key>: <value>
```

Settings are deep-merged into `config.json`. Nested objects merge recursively; scalar values replace existing keys.

## Supported settings

Any valid Scoop config key is supported. Common options include:

| Key | Type | Description |
|-----|------|-------------|
| `root_path` | string | Custom Scoop install root (e.g. `D:\scoop`) |
| `global_path` | string | Global install root for `scoop install -g` |
| `cache_path` | string | Download cache directory |
| `aria2-enabled` | boolean | Use aria2 for parallel downloads |
| `debug` | boolean | Enable Scoop debug output |
| `use_isolated_path` | boolean | Isolate Scoop from the system `PATH` |
| `last_update` | string | Managed by Scoop; usually left unchanged |

## Usage examples

### Custom install root

```yaml
extensions:
  scoop:
    settings:
      root_path: D:\scoop
      cache_path: D:\scoop\cache
```

### Enable aria2 downloads

```yaml
extensions:
  scoop:
    settings:
      aria2-enabled: true
```

### Debug mode for troubleshooting

```yaml
extensions:
  scoop:
    settings:
      debug: true
```

## Notes

- Corrupt JSON configs are backed up with a `.corrupted.<timestamp>.<suffix>` suffix and replaced with a fresh merge base.
- Writes use an atomic temp-file replace to avoid partial updates.
- Supports `dryRun` mode — logs the target path without writing.
- Config key in WinHome: `extensions.scoop`.

## Verification

After applying, inspect Scoop's config:

```powershell
scoop config
```

Or read the file directly:

```powershell
Get-Content "$env:USERPROFILE\.config\scoop\config.json"
```

Confirm your keys appear with the expected values, then run `scoop checkup` if you changed paths or download settings.
