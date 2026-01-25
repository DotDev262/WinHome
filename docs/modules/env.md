# Environment Variables

Manages user-level environment variables.

**YAML Key:** `env`

**Properties:**
-   `variable`: The name of the environment variable.
-   `value`: The value to set.
-   `action`: (Optional) `set` (default) or `append`. `append` adds the value to a path-like variable.

**Example:**
```yaml
env:
  - variable: GOPATH
    value: "%USERPROFILE%\go"
  - variable: Path
    value: "%USERPROFILE%\go\bin"
    action: append
```

