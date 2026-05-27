# gh-dash Plugin

## Description

The gh-dash plugin manages configuration for the `gh-dash` GitHub CLI extension.

The plugin can:
- Configure gh-dash dashboard settings
- Configure pull request and issue sections
- Merge YAML configuration automatically
- Detect whether gh-dash is installed
- Support dry-run configuration updates

The plugin supports both:
- Standard PyYAML parsing
- Built-in fallback YAML parsing when PyYAML is unavailable

## Supported OS

- Windows
- Linux
- macOS

## Configuration File Location

Default configuration path:

```text
~/.config/gh-dash/config.yml
```

Custom configuration path can be specified using:

```text
GH_DASH_CONFIG
```

environment variable.

---

## Configuration

Basic example:

```yaml
plugins:
  - name: gh-dash
    repoPaths:
      - "owner/repository"
```

---

## Supported Settings

The plugin supports all gh-dash YAML configuration options.

Example categories include:

| Setting | Description |
|----------|-------------|
| repoPaths | Repository list |
| prSections | Pull request dashboard sections |
| issuesSections | Issue dashboard sections |
| defaults | Default dashboard behavior |
| keybindings | Keyboard shortcuts |

Nested YAML structures and lists are supported.

---

## Usage Examples

### Example 1 — Configure Repository Paths

```yaml
plugins:
  - name: gh-dash
    repoPaths:
      - "cli/cli"
      - "dlvhdr/gh-dash"
```

### Example 2 — Configure Pull Request Sections

```yaml
plugins:
  - name: gh-dash
    prSections:
      - title: "My Pull Requests"
        filters: "is:open author:@me"
```

### Example 3 — Configure Issue Sections

```yaml
plugins:
  - name: gh-dash
    issuesSections:
      - title: "Assigned Issues"
        filters: "is:open assignee:@me"
```

### Example 4 — Configure Defaults

```yaml
plugins:
  - name: gh-dash
    defaults:
      preview:
        open: true
```

### Example 5 — Configure Keybindings

```yaml
plugins:
  - name: gh-dash
    keybindings:
      open: "o"
      refresh: "r"
```

---

## Verification

Verify gh-dash installation:

```bash
gh dash --help
```

Verify installed GitHub CLI extensions:

```bash
gh ext list
```

Verify configuration file exists:

```text
~/.config/gh-dash/config.yml
```

---

## Notes

- Existing YAML configuration is preserved and merged automatically.
- Lists are replaced completely during updates.
- Nested dictionaries are merged recursively.
- Dry-run mode is supported.
- The plugin supports both wrapped and flat configuration formats.
- If `gh-dash` executable is not found, the plugin also checks GitHub CLI extensions using:

```bash
gh ext list
```

- Temporary files are used during writes for safer configuration updates.