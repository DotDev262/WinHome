# espanso plugin

## Description

The `espanso` plugin manages Espanso text-expansion rules on Windows. It merges `matches` and `global_vars` into `%APPDATA%\espanso\match\base.yml`, allowing you to declaratively add or update triggers as part of your WinHome setup.

## Prerequisites

- Espanso must be installed ([espanso.org](https://espanso.org))
- Espanso is considered installed when `%APPDATA%\espanso` exists
- `APPDATA` must be set (standard on Windows)
- **Windows only**

## Configuration file location

| Platform | Path |
|----------|------|
| Windows  | `%APPDATA%\espanso\match\base.yml` |

## Configuration format

```yaml
extensions:
  espanso:
    matches:
      - trigger: ":email"
        replace: "you@example.com"
    global_vars:
      - name: company
        type: random
        params:
          choices:
            - "Acme Corp"
```

Top-level fields map directly to Espanso's `base.yml` structure. You do not wrap content in a `settings` object.

## Merge behavior

| Field | Merge key | Behavior |
|-------|-----------|----------|
| `matches` | `trigger` | Incoming match with the same trigger replaces the existing entry; others are preserved. New triggers are appended. |
| `global_vars` | `name` | Incoming variable with the same name replaces the existing entry; others are preserved. New variables are appended. |

Unmentioned `matches` / `global_vars` in your WinHome config are left unchanged.

## Usage examples

### Email shortcut

```yaml
extensions:
  espanso:
    matches:
      - trigger: ":sig"
        replace: |
          Best regards,
          Alex
```

### Global variable for templates

```yaml
extensions:
  espanso:
    global_vars:
      - name: username
        type: env
        params:
          var: USERNAME
```

### Update an existing trigger

Re-use the same `trigger` value to replace the rule in `base.yml`:

```yaml
extensions:
  espanso:
    matches:
      - trigger: ":date"
        replace: "{{mydate}}"
    global_vars:
      - name: mydate
        type: date
        params:
          format: "%Y-%m-%d"
```

## Notes

- The plugin uses a minimal YAML reader/writer; complex Espanso constructs should be tested after apply.
- Supports `dryRun` mode — reports changes without writing.
- Config key in WinHome: `extensions.espanso`.
- Run `espanso restart` after applying so new rules load.

## Verification

After applying:

```powershell
Get-Content "$env:APPDATA\espanso\match\base.yml"
espanso match list
```

Type a configured trigger in any app to confirm expansion works.
