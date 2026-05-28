# chocolatey plugin

## Description

The `chocolatey` plugin manages configuration for Chocolatey, a Windows package manager. It writes settings to Chocolatey's `chocolatey.config` XML file, allowing you to declaratively control both config values and feature flags as part of your WinHome setup.

## Prerequisites

- Chocolatey must be installed (see [chocolatey.org/install](https://chocolatey.org/install))
- Chocolatey is detected via `choco.exe` or `choco`
- **Windows only**

## Configuration file location

| Platform | Path |
|----------|------|
| Windows (default) | `%ChocolateyInstall%\config\chocolatey.config` |
| Windows (fallback) | `%ALLUSERSPROFILE%\chocolatey\config\chocolatey.config` |

The plugin first checks the `ChocolateyInstall` environment variable, then falls back to `%ALLUSERSPROFILE%\chocolatey`.

## Configuration format

```yaml
plugins:
  chocolatey:
    settings:
      config:
        <key>: <value>
      features:
        <feature-name>: <true|false>
```

The `config` section maps to `<add key="..." value="..."/>` entries in the XML. The `features` section maps to `<feature name="..." enabled="..."/>` entries.

## Supported settings

### Config keys

| Key | Type | Description |
|-----|------|-------------|
| `cacheLocation` | string | Custom package cache directory |
| `commandExecutionTimeoutSeconds` | integer | Timeout for command execution |
| `proxy` | string | Proxy URL for downloads |
| `proxyUser` | string | Proxy username |
| `proxyPassword` | string | Proxy password |
| `webRequestTimeoutSeconds` | integer | Web request timeout in seconds |

### Feature flags

| Feature | Type | Description |
|---------|------|-------------|
| `checksumFiles` | boolean | Verify package checksums |
| `autoUninstaller` | boolean | Auto-uninstall on package removal |
| `allowGlobalConfirmation` | boolean | Skip confirmation prompts |
| `failOnAutoUninstaller` | boolean | Fail if auto-uninstall fails |
| `useRememberedArgumentsForUpgrades` | boolean | Reuse install args on upgrade |

## Usage examples

### Example 1 — Set a custom cache location and timeout

```yaml
plugins:
  chocolatey:
    settings:
      config:
        cacheLocation: C:\choco-cache
        commandExecutionTimeoutSeconds: 2700
```

### Example 2 — Enable auto-uninstaller and skip confirmation prompts

```yaml
plugins:
  chocolatey:
    settings:
      features:
        autoUninstaller: true
        allowGlobalConfirmation: true
```

### Example 3 — Configure proxy and enable checksum verification

```yaml
plugins:
  chocolatey:
    settings:
      config:
        proxy: http://proxy.example.com:8080
        proxyUser: myuser
        proxyPassword: mypassword
      features:
        checksumFiles: true
```

## Notes

- The plugin deep-merges settings — existing config entries and features not mentioned in `config.yaml` are preserved.
- If the `chocolatey.config` file is corrupted or unparseable, the plugin automatically backs it up with a unique suffix (e.g. `chocolatey.config.corrupted.abc12345.bak`) and starts fresh.
- Setting a value to `null` is ignored (the key is skipped, not removed).
- Boolean values are written as `"true"` or `"false"` strings in the XML.
- Supports `dryRun` mode — logs what would change without writing to disk.
- **Windows only** — this plugin has no effect on other operating systems.

## Verification

After applying, check the config via Chocolatey CLI:

```bash
choco config list
choco feature list
```

Or inspect the file directly:

```bash
type "%ChocolateyInstall%\config\chocolatey.config"
```
