# Starship Plugin

## Overview

The Starship plugin configures the Starship shell prompt by updating the `starship.toml` configuration file.

It helps customize terminal prompts with themes, symbols, and performance indicators.

## Prerequisites

- Starship installed (`starship.exe` or `starship`)
- Windows environment variable support

## Configuration Schema

| Key | Type | Description |
|-----|------|-------------|
| Any TOML key | object/string | Starship configuration values |

## Usage Examples

### Basic setup
```yaml
plugins:
  - name: starship
    prompt: true
```

### Enable custom sections
```yaml
plugins:
  - name: starship
    prompt:
      add_newline: true
```

## Verification Steps

```bash
starship --version
```

Check config:

```bash
cat ~/.config/starship.toml
```

## Notes / Caveats

- Requires `starship` binary installed
- Config is automatically merged
- Runs on Windows via USERPROFILE
```
