# gh-dash Plugin

## Overview

The gh-dash plugin manages GitHub dashboard configuration for gh-dash.

## Prerequisites

- GitHub CLI installed
- gh-dash extension installed

## Configuration Schema

| Key | Type | Description |
|-----|------|-------------|
| repoPaths | array | Repositories |
| prSections | array | PR dashboard sections |
| issuesSections | array | Issue sections |

## Usage Examples

### Repo setup
```yaml
plugins:
  - name: gh-dash
    repoPaths:
      - "owner/repo"
```

### PR sections
```yaml
plugins:
  - name: gh-dash
    prSections:
      - title: "My PRs"
```

## Verification Steps

```bash
gh dash
```

```bash
gh ext list
```

## Notes / Caveats

- Uses YAML fallback parser if PyYAML missing
- Supports nested configuration
```
