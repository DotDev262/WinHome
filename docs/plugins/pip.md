# pip plugin

## Description

The `pip` plugin manages configuration for Python's package installer, pip. It writes settings to
pip's global configuration file (`pip.ini` on Windows, `pip.conf` on Linux/macOS), allowing you to
declaratively control pip's behaviour as part of your WinHome setup.

## Prerequisites

- Python and pip must be installed and available on `PATH`
- On Windows, pip is detected via `pip.exe` or `pip`

## Configuration file location

| Platform    | Path                     |
| ----------- | ------------------------ |
| Windows     | `%APPDATA%\pip\pip.ini`  |
| Linux/macOS | `~/.config/pip/pip.conf` |

## Configuration format

```yaml
plugins:
  pip:
    settings:
      <key>: <value>
```

All key-value pairs are written to the `[global]` section of the pip config file. Keys and values
map directly to pip's supported configuration options.

Setting a value to `null` removes that key from the config file.

## Supported settings

Any valid pip global configuration option is supported. Common ones include:

| Key               | Type    | Description                                  |
| ----------------- | ------- | -------------------------------------------- |
| `index-url`       | string  | Custom PyPI index URL                        |
| `extra-index-url` | string  | Additional package index URL                 |
| `trusted-host`    | string  | Mark a host as trusted (no SSL verification) |
| `timeout`         | integer | Network timeout in seconds                   |
| `retries`         | integer | Number of retries on failure                 |
| `cache-dir`       | string  | Custom cache directory                       |
| `no-cache-dir`    | boolean | Disable caching                              |
| `quiet`           | boolean | Suppress output                              |

## Usage examples

### Example 1 — Set a custom PyPI mirror

```yaml
plugins:
  pip:
    settings:
      index-url: https://pypi.tuna.tsinghua.edu.cn/simple
      trusted-host: pypi.tuna.tsinghua.edu.cn
```

### Example 2 — Set timeout and retries for slow networks

```yaml
plugins:
  pip:
    settings:
      timeout: 60
      retries: 5
```

### Example 3 — Custom cache directory and disable SSL warnings

```yaml
plugins:
  pip:
    settings:
      cache-dir: C:\pip-cache
      trusted-host: pypi.org
      no-cache-dir: false
```

### Example 4 — Remove a previously set key

```yaml
plugins:
  pip:
    settings:
      index-url: null
```

## Notes

- If the existing `pip.ini` is corrupted, the plugin automatically backs it up with a timestamp
  suffix (e.g. `pip.ini.1234567890.bak`) and starts fresh.
- The plugin preserves the case of all configuration keys.
- All settings are written under the `[global]` section only.
- Supports `dryRun` mode — logs what would change without writing to disk.

## Verification

After applying, confirm the settings were written:

```bash
pip config list
```

Or inspect the file directly:

```bash
# Windows
type %APPDATA%\pip\pip.ini
```
