# Registry Tweaks

Applies Windows Registry values idempotently. WinHome creates missing keys and skips values that already match.

**YAML Key:** `registry`

**Properties:**

| Property | Type | Description |
| --- | --- | --- |
| `path` | string | Registry key path (e.g. `HKCU\\Software\\...`) |
| `name` | string | Value name |
| `value` | string/number | Value to write |
| `type` | string | `string` (default), `dword`, `qword`, or `binary` |

Supported roots include `HKCU`, `HKLM`, `HKCR`, `HKU`, and `HKCC` (see `RegistryWrapper` for parsing rules).

---

## Basic Usage

```yaml
registry:
  - path: HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced
    name: HideFileExt
    value: 0
    type: dword
```

This shows file extensions in Explorer (`0` = show extensions).

---

## Real-World Examples

### Example 1 — Explorer and UI preferences

```yaml
registry:
  - path: HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced
    name: HideFileExt
    value: 0
    type: dword
  - path: HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced
    name: ShowSuperHidden
    value: 1
    type: dword
  - path: HKCU\Software\Microsoft\Windows\CurrentVersion\Search
    name: SearchboxTaskbarMode
    value: 1
    type: dword
```

### Example 2 — Dark mode for apps

```yaml
registry:
  - path: HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize
    name: AppsUseLightTheme
    value: 0
    type: dword
  - path: HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize
    name: SystemUsesLightTheme
    value: 0
    type: dword
```

### Example 3 — Terminal and developer QoL

```yaml
registry:
  - path: HKCU\Console
    name: QuickEdit
    value: 1
    type: dword
  - path: HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced
    name: AutoCheckSelect
    value: 1
    type: dword
```

### Example 4 — String value for default app path

```yaml
registry:
  - path: HKCU\Software\Classes\mailto
    name: ""
    value: "URL:MailTo Protocol"
    type: string
```

---

## Edge Cases

- **Idempotent apply:** Matching existing values are skipped with `[Registry] Skipped: <name> (Already set)`.
- **RegistryGuard / SYSTEM context:** Modifying `HKCU` while running as `SYSTEM` (common in CI or scheduled tasks) is blocked to avoid writing to the wrong user hive. Run WinHome as your interactive user account.
- **HKLM changes:** May require elevation depending on the key. WinHome does not auto-elevate.
- **Dry run:** `--dry-run` prints planned writes without touching the registry.
- **Revert:** The engine supports reverting registry values tracked in state; ad-hoc tweaks outside WinHome are not auto-reverted.

---

## Troubleshooting

**Issue: `Security Risk` / RegistryGuard error on HKCU**

- Do not run WinHome as SYSTEM when applying `HKCU` tweaks. Use your normal user session or a user-context CI job.

**Issue: `[Error] Could not create registry subkey`**

- Verify the `path` syntax (`HKCU\Software\...` with single backslashes in YAML).
- For `HKLM`, try an elevated terminal.

**Issue: DWORD value rejected**

- Use integers for `dword` / `qword` types (e.g. `0`, `1`, not `"0"` when possible — YAML numbers preferred).

**Issue: Change not visible until reboot**

- Some Explorer settings require restarting Explorer or signing out. Try `taskkill /f /im explorer.exe && start explorer.exe` for shell-related keys.

**Issue: Wrong user hive modified**

- Always run WinHome as the user whose profile you intend to configure. See [security.md](../security.md) for RegistryGuard details.
