# Scheduled Tasks

Creates or updates Windows Task Scheduler definitions. WinHome registers tasks under the path you specify using the Task Scheduler API.

**YAML Key:** `scheduled_tasks`

**Properties (task):**

| Property | Type | Description |
| --- | --- | --- |
| `name` | string | Display name (logged during apply) |
| `path` | string | Task Scheduler folder path (e.g. `\\WinHome\\DailyBackup`) |
| `description` | string | Task description shown in Task Scheduler |
| `author` | string | Author metadata |
| `triggers` | list | When the task runs (see below) |
| `actions` | list | What the task executes (see below) |

**Trigger properties:**

| Property | Type | Description |
| --- | --- | --- |
| `type` | string | `daily`, `weekly`, `monthly`, or `logon` |
| `enabled` | boolean | Default `true` |
| `startBoundary` | datetime | ISO-style start time (e.g. `2026-01-01T08:00:00`) |
| `endBoundary` | datetime | Optional end time |
| `executionTimeLimit` | timespan | Max runtime (e.g. `01:00:00`) |
| `repetition` | object | `interval`, `duration`, `stopAtDurationEnd` |

**Action properties:**

| Property | Type | Description |
| --- | --- | --- |
| `type` | string | Currently `exec` |
| `path` | string | Executable or script path |
| `arguments` | string | Optional command-line arguments |
| `workingDirectory` | string | Optional working directory |

---

## Basic Usage

```yaml
scheduled_tasks:
  - name: "Daily backup"
    description: "Runs a backup script every morning"
    author: "WinHome"
    path: "\\WinHome\\DailyBackup"
    triggers:
      - type: daily
        startBoundary: "2026-01-01T08:00:00"
    actions:
      - type: exec
        path: "C:\\Scripts\\backup.bat"
```

Verify in Task Scheduler (`taskschd.msc`) under the folder matching `path`.

---

## Real-World Examples

### Example 1 — Nightly maintenance script

```yaml
scheduled_tasks:
  - name: "Nightly cleanup"
    description: "Remove temp build artifacts"
    author: "WinHome"
    path: "\\WinHome\\Maintenance"
    triggers:
      - type: daily
        startBoundary: "2026-01-01T02:30:00"
        executionTimeLimit: "00:30:00"
    actions:
      - type: exec
        path: "powershell.exe"
        arguments: "-NoProfile -ExecutionPolicy Bypass -File C:\\Scripts\\cleanup.ps1"
        workingDirectory: "C:\\Scripts"
```

### Example 2 — Run on user logon

```yaml
scheduled_tasks:
  - name: "Sync dotfiles"
    description: "Pull latest dotfiles repo on login"
    author: "WinHome"
    path: "\\WinHome\\LogonSync"
    triggers:
      - type: logon
    actions:
      - type: exec
        path: "C:\\Program Files\\Git\\cmd\\git.exe"
        arguments: "-C %USERPROFILE%\\dotfiles pull --ff-only"
```

### Example 3 — Weekly report with repetition

```yaml
scheduled_tasks:
  - name: "Weekly report"
    description: "Generate status report every Monday"
    author: "WinHome"
    path: "\\WinHome\\Reports"
    triggers:
      - type: weekly
        startBoundary: "2026-01-06T09:00:00"
        repetition:
          interval: "01:00:00"
          duration: "08:00:00"
          stopAtDurationEnd: true
    actions:
      - type: exec
        path: "C:\\Scripts\\report.bat"
```

### Example 4 — PowerShell with explicit working directory

```yaml
scheduled_tasks:
  - name: "Index workspace"
    description: "Rebuild search index for dev folder"
    author: "WinHome"
    path: "\\WinHome\\Search"
    triggers:
      - type: daily
        startBoundary: "2026-01-01T23:00:00"
    actions:
      - type: exec
        path: "pwsh.exe"
        arguments: "-File .\\index.ps1"
        workingDirectory: "D:\\Dev\\tools"
```

---

## Edge Cases

- **Unsupported trigger/action types:** Only the types listed above are supported. Other values throw `NotSupportedException` at apply time.
- **Task path format:** Use backslashes and a leading `\\` folder prefix as shown. The path is passed directly to `RegisterTaskDefinition`.
- **Dry run:** `--dry-run` logs intent without registering the task.
- **Elevated tasks:** WinHome does not configure "Run with highest privileges" in the current implementation. Tasks run under the user context that executed WinHome.

---

## Troubleshooting

**Issue: Task not visible in Task Scheduler**

- Confirm `path` matches the folder tree (e.g. `\\WinHome\\DailyBackup`).
- Re-run without `--dry-run`.

**Issue: Script runs manually but not from the task**

- Use absolute paths for executables and scripts.
- Set `workingDirectory` when the script relies on relative paths.
- Check Task Scheduler → task → History for error codes.

**Issue: `logon` trigger fires for every user**

- Tasks are registered in the current user's task folder unless configured otherwise. Run WinHome as the intended user account.

**Issue: Repetition not behaving as expected**

- `interval` and `duration` use `TimeSpan` syntax (`HH:MM:SS`). Ensure `startBoundary` is in the future or reset for testing.
