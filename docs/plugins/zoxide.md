# Zoxide Plugin

## Description

The Zoxide plugin configures and initializes Zoxide shell integration for PowerShell and Bash.

The plugin can:
- Configure Zoxide initialization
- Configure environment variables
- Automatically update PowerShell and Bash profiles
- Detect whether Zoxide is installed
- Support dry-run configuration updates

## Supported OS

- Windows
- Linux
- macOS

## Profile Locations

### PowerShell

```text
%USERPROFILE%\Documents\PowerShell\Microsoft.PowerShell_profile.ps1
```

### Bash

```text
~/.bashrc
```

or

```text
~/.bash_profile
```

## Configuration

Basic configuration example:

```yaml
plugins:
  - name: zoxide
    init: {}
```

## Supported Settings

### Environment Variables

| Variable | Description |
|----------|-------------|
| _ZO_MAX_DEPTH | Maximum search depth |
| _ZO_ECHO | Print matched directory before navigation |
| _ZO_EXCLUDE_DIRS | Excluded directories |
| _ZO_RESOLVE_SYMLINKS | Resolve symbolic links |

### Init Options

| Setting | Description |
|----------|-------------|
| cmd | Custom command alias |
| hook | Shell hook type |
| no_cmd | Disable command alias |

---

## Usage Examples

### Example 1 — Basic Initialization

```yaml
plugins:
  - name: zoxide
    init: {}
```

### Example 2 — Custom Command Alias

```yaml
plugins:
  - name: zoxide
    init:
      cmd: "z"
```

### Example 3 — Configure Environment Variables

```yaml
plugins:
  - name: zoxide
    env_vars:
      _ZO_MAX_DEPTH: 5
      _ZO_ECHO: 1
```

### Example 4 — Disable Default Command

```yaml
plugins:
  - name: zoxide
    init:
      no_cmd: true
```

### Example 5 — Configure Hook Type

```yaml
plugins:
  - name: zoxide
    init:
      hook: "prompt"
```

---

## Verification

Verify Zoxide installation:

```bash
zoxide --version
```

Verify shell integration:

```bash
zoxide query
```

Verify PowerShell profile contains initialization:

```powershell
Get-Content $PROFILE
```

Verify Bash profile:

```bash
cat ~/.bashrc
```

---

## Notes

- The plugin automatically updates PowerShell and Bash profiles.
- Existing Zoxide initialization lines are replaced automatically.
- Windows environment variables are configured using `setx`.
- Dry-run mode is supported.
- Bash initialization uses:

```bash
eval "$(zoxide init bash)"
```

- PowerShell initialization uses:

```powershell
Invoke-Expression (& { (zoxide init powershell) })
```