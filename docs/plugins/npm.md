# npm plugin

## Description

The `npm` plugin manages configuration for Node.js's package manager, npm. It writes settings to the user-level `.npmrc` file, allowing you to declaratively control npm's behaviour as part of your WinHome setup.

## Prerequisites

- Node.js and npm must be installed and available on `PATH`
- On Windows, npm is detected via `npm.cmd` or `npm`
- The `USERPROFILE` environment variable must be set (standard on Windows)

## Configuration file location

| Platform | Path |
|----------|------|
| Windows  | `%USERPROFILE%\.npmrc` |

## Configuration format

```yaml
plugins:
  npm:
    <key>: <value>
```

All key-value pairs are written directly to the `.npmrc` file in `key=value` format. Keys map directly to npm's supported configuration options.

## Supported settings

Any valid npm configuration option is supported. Common ones include:

| Key | Type | Description |
|-----|------|-------------|
| `registry` | string | Custom npm registry URL |
| `cache` | string | Custom cache directory path |
| `prefix` | string | Global install prefix directory |
| `strict-ssl` | boolean | Enable/disable SSL verification |
| `save-exact` | boolean | Save exact version instead of range |
| `fund` | boolean | Show funding messages |
| `audit` | boolean | Enable/disable audit on install |
| `loglevel` | string | Log level (`silent`, `error`, `warn`, `info`, `verbose`) |

## Usage examples

### Example 1 — Set a custom npm registry

```yaml
plugins:
  npm:
    registry: https://registry.npmmirror.com
```

### Example 2 — Configure cache and disable SSL for a private registry

```yaml
plugins:
  npm:
    registry: https://my-private-registry.example.com
    strict-ssl: false
    cache: C:\npm-cache
```

### Example 3 — Clean install settings for CI environments

```yaml
plugins:
  npm:
    save-exact: true
    audit: false
    fund: false
    loglevel: warn
```

## Notes

- The plugin reads the existing `.npmrc` and merges your settings — existing keys not mentioned in `config.yaml` are preserved.
- Boolean values are converted to `"true"` or `"false"` strings as required by the `.npmrc` format.
- Lines starting with `;` in `.npmrc` are treated as comments and are preserved on read but not written back.
- Supports `dryRun` mode — logs what would change without writing to disk.
- The `USERPROFILE` environment variable must be set; the plugin will error if it is missing.

## Verification

After applying, confirm the settings were written:

```bash
npm config list
```

Or inspect the file directly:

```bash
# Windows
type %USERPROFILE%\.npmrc
```
