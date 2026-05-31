# ripgrep plugin

## Description

The `ripgrep` plugin manages ripgrep (`rg`) default flags in a `.ripgreprc` file. Settings are written as `--flag` or `--key=value` lines so every `rg` invocation picks up your preferences without repeating flags on the command line.

## Prerequisites

- ripgrep installed (`rg.exe` or `rg` on `PATH`)
- On Windows, `USERPROFILE` must be set unless `RIPGREP_CONFIG_PATH` overrides the location

## Configuration file location

| Platform | Path |
|----------|------|
| Windows (default) | `%USERPROFILE%\.ripgreprc` |
| Custom | Value of `RIPGREP_CONFIG_PATH` when set |

## Configuration format

Settings use ripgrep flag names **without** the leading `--`:

```yaml
extensions:
  ripgrep:
    settings:
      smart-case: true
      hidden: true
```

Boolean `true` becomes a standalone `--flag` line. Other values become `--key=value`.

## Supported settings

Any valid ripgrep flag name is accepted. Common options:

| Key | Type | Description |
|-----|------|-------------|
| `smart-case` | bool | Case-insensitive when pattern is all lowercase |
| `hidden` | bool | Search hidden files and directories |
| `ignore-case` | bool | Case-insensitive search |
| `glob` | string | Glob filter (e.g. `!node_modules`) |
| `type` | string | Restrict to file type (e.g. `rust`) |
| `max-columns` | string | Truncate long lines |
| `line-number` | bool | Show line numbers (default for rg) |

Set a flag to `false` in WinHome config to remove it from `.ripgreprc`.

## Usage examples

### Everyday search defaults

```yaml
extensions:
  ripgrep:
    settings:
      smart-case: true
      hidden: false
      glob: "!node_modules"
```

### Type-specific defaults

```yaml
extensions:
  ripgrep:
    settings:
      type: rust
      smart-case: true
```

### Column limit for large files

```yaml
extensions:
  ripgrep:
    settings:
      max-columns: "200"
      max-columns-preview: true
```

## Notes

- Corrupted config files are moved aside with a `.corrupted.<timestamp>` suffix before rewrite.
- Unknown or invalid lines in an existing file trigger a fresh start after backup.
- Supports `dryRun`.
- Config key in WinHome: `extensions.ripgrep`.

## Verification

After applying:

```powershell
Get-Content $env:USERPROFILE\.ripgreprc
rg --help
# Run rg in a repo and confirm defaults (e.g. smart-case) apply without extra flags
```
