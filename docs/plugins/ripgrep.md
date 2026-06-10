# Ripgrep Plugin

## Overview

The Ripgrep plugin manages configuration settings for the ripgrep search tool (`rg`).

## Prerequisites

- Ripgrep installed (`rg.exe` or `rg` in PATH)

## Configuration Schema

Config is managed via the file pointed to by the `RIPGREP_CONFIG_PATH` environment variable. If not set, it defaults to:
- Windows: `%USERPROFILE%\.ripgreprc`
- Linux/macOS: `~/.ripgreprc`

The plugin accepts arbitrary ripgrep configuration flags in the `settings` key. 

- Boolean keys (e.g., `smart-case: true`) are written as bare flags (`--smart-case`). Setting to `false` removes the flag.
- String or numeric keys are written as option-value assignments (e.g. `max-columns: 150` becomes `--max-columns=150`).

## Usage Examples

```yaml
plugins:
  - name: ripgrep
    settings:
      smart-case: true
      max-columns: 150
      glob: "!*.min.js"
```

## Verification Steps

To verify the installed version:
```bash
rg --version
```
To verify settings, check the contents of your ripgrep config file:
```bash
cat ~/.ripgreprc
```

## Notes / Caveats

- For ripgrep to utilize the config file during command-line sessions, ensure that the `RIPGREP_CONFIG_PATH` environment variable is defined in your shell profile.
- A backup of the existing config file is automatically created before any writes.
