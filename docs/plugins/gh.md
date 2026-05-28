# GitHub CLI Plugin

## Overview

The GitHub CLI plugin manages configuration for `gh` using YAML config file.

## Prerequisites

- GitHub CLI installed (`gh`)
- PyYAML installed

## Configuration Schema

| Key | Type | Description |
|-----|------|-------------|
| git_protocol | string | SSH or HTTPS |
| editor | string | Default editor |
| aliases | object | Command aliases |

## Usage Examples

### Git protocol
```yaml
plugins:
  - name: gh
    git_protocol: ssh
```

### Aliases
```yaml
plugins:
  - name: gh
    aliases:
      co: pr checkout
```

## Verification Steps

```bash
gh --version
```

```bash
gh config list
```

## Notes / Caveats

- YAML is auto-merged
- Empty values ignored
```
