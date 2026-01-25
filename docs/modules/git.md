# Git

Configures global Git settings.

**YAML Key:** `git`

**Properties:**
-   `userName`: Your Git user name.
-   `userEmail`: Your Git user email.
-   `signingKey`: Your GPG signing key.
-   `commitGpgSign`: `true` or `false`.
-   `settings`: A dictionary of any other Git config key/value pairs.

**Example:**
```yaml
git:
  userName: "Your Name"
  userEmail: "your.email@example.com"
  signingKey: "ABC12345"
  commitGpgSign: true
  settings:
    init.defaultBranch: main
    pull.rebase: true
```
