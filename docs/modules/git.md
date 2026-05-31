# Git

Configures global Git settings through `git config --global`. WinHome applies identity, signing, and arbitrary config keys idempotently — unchanged values are skipped.

**YAML Key:** `git`

**Properties:**

| Property | Type | Description |
| --- | --- | --- |
| `userName` | string | Sets `user.name` |
| `userEmail` | string | Sets `user.email` |
| `signingKey` | string | Sets `user.signingkey` (GPG key ID or path) |
| `commitGpgSign` | boolean | Sets `commit.gpgsign` |
| `settings` | object | Any other global Git config key/value pairs |

---

## Basic Usage

Install Git first (via `winget`, `scoop`, or another manager), then declare your identity:

```yaml
git:
  userName: "Alex Chen"
  userEmail: "alex@example.com"
```

Verify after apply:

```powershell
git config --global user.name
git config --global user.email
```

---

## Real-World Examples

### Example 1 — Developer defaults

```yaml
git:
  userName: "Alex Chen"
  userEmail: "alex@example.com"
  settings:
    init.defaultBranch: main
    pull.rebase: true
    core.autocrlf: true
    core.editor: "code --wait"
```

### Example 2 — Signed commits with GPG

```yaml
git:
  userName: "Alex Chen"
  userEmail: "alex@example.com"
  signingKey: "ABCD1234EF567890"
  commitGpgSign: true
  settings:
    gpg.program: "C:\\Program Files\\GnuPG\\bin\\gpg.exe"
```

### Example 3 — Line-ending and merge preferences

```yaml
git:
  settings:
    core.autocrlf: input
    merge.conflictstyle: diff3
    fetch.prune: true
    rebase.autoStash: true
```

### Example 4 — Credential helper on Windows

```yaml
git:
  settings:
    credential.helper: manager
    credential.https://github.com.helper: manager
```

---

## Edge Cases

- **Git not installed:** WinHome logs an error and skips the module. Install Git via a package manager entry in the same `config.yaml` before relying on this block.
- **Scoop installs:** WinHome falls back to common Scoop shim paths when `git` is not on `PATH` yet in the current shell.
- **Settings dictionary keys:** Use dotted Git config names (`core.editor`, `init.defaultBranch`). Values are passed verbatim to `git config --global`.
- **Dry run:** Use `--dry-run` to preview changes without writing to `~/.gitconfig`.

---

## Troubleshooting

**Issue: `[Git] Error: Git is not installed/found in PATH`**

- Install Git (`winget install Git.Git` or `scoop install git`).
- Open a new terminal so `PATH` includes Git, then re-run WinHome.

**Issue: Setting appears unchanged**

- WinHome skips keys that already match the desired value (case-insensitive compare).
- Run `git config --global --list` to inspect current values.

**Issue: GPG signing fails after apply**

- Confirm `gpg.program` points to your GPG executable.
- Test manually: `gpg --list-secret-keys` and `git commit -S --allow-empty -m test`.

**Issue: `core.editor` not picked up in VS Code terminal**

- Some editors require a full path or `--wait` flag. Example: `"C:\\Program Files\\Microsoft VS Code\\Code.exe" --wait`.
