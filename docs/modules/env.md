# Environment Variables

Manages **user-level** environment variables on Windows. WinHome writes to the `HKCU\Environment` hive — no administrator rights required for standard user variables.

**YAML Key:** `envVars`

**Properties:**

| Property | Type | Description |
| --- | --- | --- |
| `variable` | string | Environment variable name (e.g. `Path`, `GOPATH`) |
| `value` | string | Value to set or append. Supports `%USERPROFILE%` expansion |
| `action` | string | `set` (default) replaces the variable; `append` adds to a semicolon-separated list |

Profile-specific overrides live under `profiles.<name>.envVars` and apply when you run with `--profile <name>`.

---

## Basic Usage

```yaml
envVars:
  - variable: GOPATH
    value: "%USERPROFILE%\\go"
  - variable: Path
    value: "%USERPROFILE%\\go\\bin"
    action: append
```

After apply, open a **new** terminal session for apps to see updated values.

---

## Real-World Examples

### Example 1 — Go toolchain paths

```yaml
envVars:
  - variable: GOPATH
    value: "%USERPROFILE%\\go"
  - variable: GOBIN
    value: "%USERPROFILE%\\go\\bin"
  - variable: Path
    value: "%USERPROFILE%\\go\\bin"
    action: append
```

### Example 2 — Node.js custom prefix

```yaml
envVars:
  - variable: NPM_CONFIG_PREFIX
    value: "%USERPROFILE%\\npm-global"
  - variable: Path
    value: "%USERPROFILE%\\npm-global"
    action: append
```

### Example 3 — Developer editor and pager defaults

```yaml
envVars:
  - variable: EDITOR
    value: nvim
  - variable: VISUAL
    value: nvim
  - variable: PAGER
    value: less
```

### Example 4 — Work profile with separate bin directory

```yaml
envVars:
  - variable: EDITOR
    value: code

profiles:
  work:
    envVars:
      - variable: EDITOR
        value: code
        action: set
      - variable: Path
        value: "%USERPROFILE%\\work\\bin"
        action: append
```

Run with: `WinHome --config config.yaml --profile work`

---

## Edge Cases

- **Append idempotency:** `append` skips when the exact segment already exists (case-insensitive), so re-running WinHome is safe.
- **Path separator:** Windows uses `;` between Path entries. Do not use `:` (Unix style).
- **User scope only:** Machine-wide (`HKLM`) variables are not modified. Use Group Policy or an elevated tool for system-wide changes.
- **Process PATH refresh:** WinHome refreshes the current process `Path` after apply, but already-running apps keep their old environment until restarted.

---

## Troubleshooting

**Issue: Variable not visible in a new app**

- User env changes require a new process. Fully close and reopen the terminal or IDE.
- Check Windows Settings → System → About → Advanced system settings → Environment Variables (User section).

**Issue: `append` did nothing**

- The value may already be present. WinHome logs `[Env] Skipped: '<value>' already in <variable>`.
- Verify spelling and `%USERPROFILE%` expansion match the existing entry.

**Issue: Path entry points to a missing folder**

- WinHome does not validate that directories exist. Create the folder first or fix the path in `config.yaml`.

**Issue: Profile env vars not applied**

- Confirm you passed `--profile <name>` and that the profile key matches `profiles` in YAML exactly.
