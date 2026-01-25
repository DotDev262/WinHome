# Scheduled Tasks

Configures scheduled tasks.

**YAML Key:** `scheduled_tasks`

**Properties:**
-   `name`: The name of the task.
-   `description`: A description for the task.
-   `author`: The author of the task.
-   `path`: The path for the task definition.
-   `triggers`: A list of triggers for the task.
-   `actions`: A list of actions for the task.

**Example:**
```yaml
scheduled_tasks:
  - name: "My Daily Task"
    description: "Runs a script every day."
    author: "WinHome"
    path: "\\MyTasks\\DailyScript"
    triggers:
      - type: "daily"
        startBoundary: "2024-01-01T08:00:00"
    actions:
      - type: "exec"
        path: "C:\\path\\to\\my\\script.bat"
```

