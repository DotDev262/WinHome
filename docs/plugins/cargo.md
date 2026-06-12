# cargo plugin

## Description

The `cargo` plugin manages configuration for Rust's package manager, Cargo. It writes settings to
the user-level `config.toml` file, allowing you to declaratively control Cargo's behaviour as part
of your WinHome setup.

## Prerequisites

- Rust and Cargo must be installed (via [rustup](https://rustup.rs))
- Cargo is detected via `cargo.exe`, `cargo`, or `%CARGO_HOME%\bin\cargo.exe`
- Python 3.11+ is required (uses built-in `tomllib`), or install `tomli` for older Python versions

## Configuration file location

| Platform | Path                               |
| -------- | ---------------------------------- |
| Windows  | `%USERPROFILE%\.cargo\config.toml` |

## Configuration format

```yaml
plugins:
  cargo:
    settings:
      <section>:
        <key>: <value>
```

Settings are written to the TOML config file. Nested objects become TOML sections (e.g. `[net]`,
`[build]`). Top-level non-object values are written as top-level TOML keys.

## Supported settings

Any valid Cargo configuration option is supported. Common sections include:

| Section    | Key                  | Type    | Description                            |
| ---------- | -------------------- | ------- | -------------------------------------- |
| `net`      | `retry`              | integer | Number of retries for network requests |
| `net`      | `offline`            | boolean | Prevent network access                 |
| `net`      | `git-fetch-with-cli` | boolean | Use system git for fetching            |
| `build`    | `jobs`               | integer | Number of parallel build jobs          |
| `build`    | `target-dir`         | string  | Custom build output directory          |
| `http`     | `timeout`            | integer | HTTP timeout in seconds                |
| `http`     | `proxy`              | string  | HTTP proxy URL                         |
| `registry` | `default`            | string  | Default registry name                  |

## Usage examples

### Example 1 — Configure network retries and offline mode

```yaml
plugins:
  cargo:
    settings:
      net:
        retry: 5
        offline: false
```

### Example 2 — Speed up builds with parallel jobs and custom target directory

```yaml
plugins:
  cargo:
    settings:
      build:
        jobs: 8
        target-dir: C:\cargo-target
```

### Example 3 — Set an HTTP proxy for corporate environments

```yaml
plugins:
  cargo:
    settings:
      http:
        proxy: http://proxy.example.com:8080
        timeout: 30
      net:
        git-fetch-with-cli: true
```

## Notes

- Settings are deep-merged — existing config keys not mentioned in `config.yaml` are preserved.
- Requires `tomllib` (Python 3.11+) or `tomli` package for reading TOML files. The plugin raises a
  `RuntimeError` if neither is available.
- The plugin writes a simple TOML serialiser — complex nested structures beyond two levels may not
  be supported.
- Supports `dryRun` mode — logs what would change without writing to disk.
- The `USERPROFILE` environment variable must be set.

## Verification

After applying, inspect the config file directly:

```bash
# Windows
type %USERPROFILE%\.cargo\config.toml
```

Or verify cargo is picking up the settings:

```bash
cargo config get net.retry
```
