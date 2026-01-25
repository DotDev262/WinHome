# Registry Tweaks

Applies Windows Registry tweaks.

**YAML Key:** `registry`

**Properties:**
-   `path`: The registry key path (e.g., `HKCU\\Software\\...`).
-   `name`: The value name.
-   `value`: The value to set.
-   `type`: `string` (default), `dword`, `qword`, or `binary`.

**Example:**
```yaml
registry:
  - path: HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced
    name: HideFileExt
    value: 0
    type: dword
```

