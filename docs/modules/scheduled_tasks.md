# Scheduled Tasks

Configures Windows Task Scheduler to run automated tasks on your system.

**YAML Key:** `scheduled_tasks`

**Properties:**

- `name` : Name of the scheduled task.
- `command` : Command to run.
- `trigger` : When to run the task.
- `enabled` : `true` or `false`.

---

## Basic Usage

```yaml
scheduled_tasks:
  - name: 'DailyBackup'
    command: "C:\\scripts\\backup.bat"
    trigger: 'daily'
    enabled: true
```

---

## Real-World Examples

### Example 1 — Daily Backup

```yaml
scheduled_tasks:
  - name: 'DailyBackup'
    command: "C:\\scripts\\backup.bat"
    trigger: 'daily'
    enabled: true
```

### Example 2 — Weekly Cleanup

```yaml
scheduled_tasks:
  - name: 'WeeklyCleanup'
    command: "C:\\scripts\\cleanup.bat"
    trigger: 'weekly'
    enabled: true
```

### Example 3 — System Update Check

```yaml
scheduled_tasks:
  - name: 'UpdateCheck'
    command: "C:\\scripts\\update.bat"
    trigger: 'weekly'
    enabled: true
```

### Example 4 — Multiple Tasks

```yaml
scheduled_tasks:
  - name: 'DailyBackup'
    command: "C:\\scripts\\backup.bat"
    trigger: 'daily'
    enabled: true
  - name: 'WeeklyCleanup'
    command: "C:\\scripts\\cleanup.bat"
    trigger: 'weekly'
    enabled: true
```

---

## Troubleshooting

**Issue: Task not running**

- Make sure WinHome is run as Administrator
- Check if task is enabled in Task Scheduler
- Open Task Scheduler to verify task exists

**Issue: Command not found**

- Check if the script path is correct
- Use full path for commands
- Make sure script file exists

**Issue: Task runs but fails**

- Check script for errors
- Run script manually first to test
- Check Task Scheduler logs for errors
