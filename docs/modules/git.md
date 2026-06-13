# Git

Configures global Git settings for your Windows environment. This module sets your Git identity and
preferences so every machine is ready to commit.

**YAML Key:** `git`

**Properties:**

- `userName` : Your Git user name.
- `userEmail` : Your Git user email.
- `signingKey` : Your GPG signing key.
- `commitGpgSign` : `true` or `false`.
- `settings` : A dictionary of any other Git config key/value pairs.

---

## Basic Usage

To configure Git using WinHome, add the `git` key to your `config.yaml`:

```yaml
git:
  userName: 'Your Name'
  userEmail: 'your.email@example.com'
  settings:
    init.defaultBranch: main
    pull.rebase: true
```

---

## Advanced Configuration

### Enabling GPG Commit Signing

```yaml
git:
  userName: 'Your Name'
  userEmail: 'your.email@example.com'
  signingKey: 'ABC12345'
  commitGpgSign: true
```

### Custom Git Settings

```yaml
git:
  userName: 'Your Name'
  userEmail: 'your.email@example.com'
  settings:
    init.defaultBranch: main
    pull.rebase: true
    core.autocrlf: true
    core.editor: code --wait
```

---

## Real-World config.yaml Examples

### Example 1 — Developer Setup

```yaml
git:
  userName: 'John Doe'
  userEmail: 'john@example.com'
  settings:
    init.defaultBranch: main
    pull.rebase: true
    core.editor: code --wait
```

### Example 2 — Work Profile Setup

```yaml
git:
  userName: 'John Work'
  userEmail: 'john@company.com'
  settings:
    core.autocrlf: true
    credential.helper: manager
    http.proxy: http://proxy.company.com
```

### Example 3 — Secure Setup With GPG Signing

```yaml
git:
  userName: 'John Doe'
  userEmail: 'john@example.com'
  signingKey: 'ABC12345'
  commitGpgSign: true
  settings:
    init.defaultBranch: main
    pull.rebase: true
```

### Example 4 — Minimal Setup

```yaml
git:
  userName: 'John Doe'
  userEmail: 'john@example.com'
  settings:
    init.defaultBranch: main
```

---

## Troubleshooting

**Issue: Git is not recognized as a command**

- Make sure Git is installed on your system
- Download from https://git-scm.com
- Restart terminal after installation

**Issue: GPG signing failed**

- Check if GPG is installed on your system
- Verify your signing key is correct
- Run `gpg --list-keys` to see available keys

**Issue: Wrong email showing in commits**

- Make sure email matches your GitHub account
- Run `git log --oneline` to verify commits

**Issue: Changes not applying**

- Make sure WinHome is run as Administrator
- Re-run WinHome after making changes
