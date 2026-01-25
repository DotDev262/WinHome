# Windows Services

Manages Windows services.

**YAML Key:** `win_services`

**Properties:**
-   `name`: The service name.
-   `startupType`: `auto`, `demand`, or `disabled`.
-   `state`: `running` or `stopped`.

**Example:**
```yaml
win_services:
  - name: "Spooler"
    startupType: "disabled"
    state: "stopped"
```
