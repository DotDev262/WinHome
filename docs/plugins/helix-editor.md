# Helix Editor Plugin

## Overview

The Helix Editor plugin manages Helix editor configuration files.

It can:

- Configure Helix editor settings
- Configure language-specific settings
- Update `config.toml`
- Update `languages.toml`
- Merge new settings with existing configuration

## Prerequisites

- Helix Editor installed
- `hx` command available in PATH
- Windows environment with `%APPDATA%` configured
- Permission to modify Helix configuration files

## Configuration Schema

| Field | Type | Default | Description |
|---------|---------|---------|---------|
| config | object | none | General Helix editor settings |
| languages | object | none | Language-specific configuration |

### Config Example

```yaml
config:
  theme: "onedark"
  editor:
    line-number: "relative"
```

### Languages Example

```yaml
languages:
  language:
    - name: python
      auto-format: true
```

## Usage Examples

### Basic Editor Configuration

```yaml
config:
  theme: "onedark"
  editor:
    line-number: "relative"
```

### Configure Python Language Support

```yaml
languages:
  language:
    - name: python
      auto-format: true
```

### Multiple Language Configuration

```yaml
languages:
  language:
    - name: python
      auto-format: true

    - name: rust
      auto-format: true
```

## Verification Steps

1. Apply the configuration.
2. Open the Helix editor.
3. Verify `%APPDATA%\helix\config.toml` exists.
4. Verify `%APPDATA%\helix\languages.toml` exists if language settings were configured.
5. Confirm editor settings are applied.
6. Confirm language-specific settings are active.

## Notes / Caveats

- Existing configuration is merged with new settings.
- Configuration is stored in TOML format.
- Language-specific settings are written to `languages.toml`.
- General editor settings are written to `config.toml`.
