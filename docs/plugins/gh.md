# GitHub CLI Plugin

## Description

The GitHub CLI plugin manages GitHub CLI (`gh`) configuration by updating the GitHub CLI YAML configuration file.

The plugin can:
- Configure GitHub CLI settings
- Merge new configuration into existing settings
- Automatically create configuration directories
- Detect whether GitHub CLI is installed
- Support dry-run configuration updates

## Supported OS

- Windows
- Linux
- macOS

## Configuration File Location

### Windows

```text
%APPDATA%\GitHub CLI\config.yml
```

## Configuration Format

The plugin stores settings in YAML format.

Basic example:

```yaml
plugins:
  - name: gh
    git_protocol: ssh
```

---

## Supported Settings

The plugin supports GitHub CLI configuration keys.

Example settings include:

| Setting | Description |
|----------|-------------|
| git_protocol | Preferred Git protocol |
| editor | Default editor |
| browser | Default browser |
| prompt | Interactive prompt behavior |
| aliases | GitHub CLI aliases |

Nested configuration objects are also supported.

---

## Usage Examples

### Example 1 — Configure Git Protocol

```yaml
plugins:
  - name: gh
    git_protocol: ssh
```

### Example 2 — Configure Default Editor

```yaml
plugins:
  - name: gh
    editor: vscode
```

### Example 3 — Configure Aliases

```yaml
plugins:
  - name: gh
    aliases:
      co: "pr checkout"
      pv: "pr view"
```

### Example 4 — Configure Browser

```yaml
plugins:
  - name: gh
    browser: chrome
```

### Example 5 — Nested Configuration

```yaml
plugins:
  - name: gh
    prompt: enabled
```

---

## Verification

Verify GitHub CLI installation:

```bash
gh --version
```

Verify current configuration:

```bash
gh config list
```

Verify the configuration file exists:

```text
%APPDATA%\GitHub CLI\config.yml
```

---

## Notes

- Existing YAML configuration is preserved and merged automatically.
- Empty values are ignored during updates.
- The plugin automatically creates missing configuration directories.
- Dry-run mode is supported.
- PyYAML is required for reading and writing configuration files.
- Nested YAML configuration objects are supported.