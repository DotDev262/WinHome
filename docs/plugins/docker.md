# docker plugin

## Description

The `docker` plugin manages Docker Desktop settings on Windows. It deep-merges JSON into Docker Desktop's `settings.json`, allowing you to declaratively control resource limits, WSL integration, and other Desktop options as part of your WinHome setup.

## Prerequisites

- Docker Desktop must be installed
- Docker is detected via `docker.exe` or `docker` on `PATH`
- `APPDATA` must be set (standard on Windows)
- **Windows only**

## Configuration file location

| Platform | Path |
|----------|------|
| Windows  | `%APPDATA%\Docker\settings.json` |

## Configuration format

```yaml
extensions:
  docker:
    settings:
      <key>: <value>
```

Settings are deep-merged into `settings.json`. Nested objects merge recursively; scalar values replace existing keys.

## Supported settings

Any valid Docker Desktop settings key is supported. Common options include:

| Key | Type | Description |
|-----|------|-------------|
| `wslEngineEnabled` | boolean | Use WSL 2 backend |
| `useVirtualizationFrameworkVirtioFS` | boolean | VirtioFS file sharing (macOS-oriented; ignored on Windows when absent) |
| `memoryMiB` | integer | Memory limit for the Docker VM |
| `cpus` | integer | CPU count allocated to Docker |
| `diskSizeMiB` | integer | Disk image size |
| `autoStart` | boolean | Start Docker Desktop on login |
| `displaySwitch` | boolean | Show tray icon / UI preferences |
| `analyticsEnabled` | boolean | Usage analytics toggle |

Refer to Docker Desktop documentation for the full schema — the plugin does not validate keys.

## Usage examples

### Resource limits

```yaml
extensions:
  docker:
    settings:
      memoryMiB: 8192
      cpus: 4
```

### Disable analytics

```yaml
extensions:
  docker:
    settings:
      analyticsEnabled: false
```

### WSL integration preference

```yaml
extensions:
  docker:
    settings:
      wslEngineEnabled: true
```

## Notes

- Corrupt JSON is backed up with a `.corrupted.<timestamp>.<suffix>` suffix before starting fresh.
- Writes use a temp file and atomic replace.
- Supports `dryRun` mode — logs the target path without writing.
- Config key in WinHome: `extensions.docker`.
- Restart Docker Desktop after changing resource or engine settings.

## Verification

After applying, inspect the settings file:

```powershell
Get-Content "$env:APPDATA\Docker\settings.json" | ConvertFrom-Json
```

Confirm your keys were merged, then restart Docker Desktop if required for the change to take effect.
