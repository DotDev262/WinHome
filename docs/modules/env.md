# Environment Variables

Manages user-level environment variables.

**YAML Key:** `envVars`

**Properties:**

- `variable`: The name of the environment variable.
- `value`: The value to set.
- `action`: (Optional) `set` (default) or `append`. `append` adds the value to a path-like variable.

**Example:**

```yaml
envVars:
  - variable: GOPATH
    value: "%USERPROFILE%\go"
  - variable: Path
    value: "%USERPROFILE%\go\bin"
    action: append
```

Profile-specific environment variables can be declared under `profiles.<name>.envVars`. When that
profile is selected with `--profile`, `set` entries replace matching top-level variables, while
`append` entries add profile-only path segments.

```yaml
envVars:
  - variable: EDITOR
    value: nvim
    action: set

profiles:
  work:
    envVars:
      - variable: EDITOR
        value: code
        action: set
      - variable: Path
        value: "%USERPROFILE%\work\bin"
        action: append
```
