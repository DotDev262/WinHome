# Starship Plugin

## Description

The Starship plugin manages the Starship prompt configuration by updating the `starship.toml` file inside the user's configuration directory.

The plugin can:
- Check whether Starship is installed
- Apply and merge Starship configuration settings
- Create the config file automatically if it does not exist

## Supported OS

- Windows
- Linux
- macOS

## Configuration File Location

```text
~/.config/starship.toml
```

On Windows:

```text
%USERPROFILE%\.config\starship.toml
```

## Configuration

Example configuration:

```yaml
plugins:
  - name: starship
    settings:
      add_newline: false
```

## Supported Settings

The plugin supports Starship TOML configuration settings.

Example:

```yaml
plugins:
  - name: starship
    settings:
      add_newline: false
      command_timeout: 1000
```

Nested TOML tables are also supported:

```yaml
plugins:
  - name: starship
    settings:
      character:
        success_symbol: "[➜](bold green)"
```

## Usage Examples

### Example 1 — Disable Newline

```yaml
plugins:
  - name: starship
    settings:
      add_newline: false
```

### Example 2 — Configure Prompt Symbol

```yaml
plugins:
  - name: starship
    settings:
      character:
        success_symbol: "[➜](bold green)"
```

### Example 3 — Set Command Timeout

```yaml
plugins:
  - name: starship
    settings:
      command_timeout: 1000
```

## Verification

Verify Starship installation:

```bash
starship --version
```

Verify the configuration file exists:

```bash
cat ~/.config/starship.toml
```

## Notes

- Existing configuration values are preserved and merged with new settings.
- The plugin automatically creates the configuration directory if it does not exist.
- Python 3.11+ is recommended for TOML parsing support.