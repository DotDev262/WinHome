# Plugin Development Guide

WinHome v1.2 introduces a **Process-Based Plugin Architecture**, allowing you to extend WinHome using any language (Python, JavaScript/TypeScript, Go, Rust, etc.).

## 1. How Plugins Work
WinHome plugins are standalone folders located in `%LOCALAPPDATA%\WinHome\plugins`.
When WinHome runs, it scans these folders for a `plugin.yaml` manifest.

Communication happens via **JSON over Standard I/O**:
1.  WinHome spawns your plugin process.
2.  WinHome writes a **Request** JSON object to your `stdin`.
3.  Your plugin prints a **Response** JSON object to `stdout`.
4.  Logs/Debug info should be printed to `stderr` (which WinHome captures and logs).

## 2. The Manifest (`plugin.yaml`)

Every plugin must have a `plugin.yaml` file at its root.

```yaml
name: my-awesome-plugin
version: 1.0.0
type: python          # Options: python, javascript, typescript, executable
main: src/main.py     # Entry point script
capabilities:
  - package_manager   # If you want to install apps (e.g., npm, cargo, pip)
  - config_provider   # If you want to handle a section in config.yaml
```

### Supported Types
*   `python`: Executed via `uv run --quiet <main>`. WinHome automatically manages the `uv` runtime.
*   `javascript` / `typescript`: Executed via `bun run <main>`. WinHome automatically manages the `bun` runtime.
*   `executable`: Executed directly. Useful for compiled binaries (Go, Rust, C#).

## 3. The Protocol (JSON)

### Request (Input)
```json
{
  "requestId": "uuid...",
  "command": "install",  // The action to perform
  "args": {              // Arguments specific to the command
    "packageId": "requests",
    "version": "1.0"
  },
  "context": {           // Global context
    "dryRun": true
  }
}
```

### Response (Output)
```json
{
  "requestId": "uuid...", // Must match request
  "success": true,        // true/false
  "changed": true,        // Did the system state change?
  "error": null,          // Error message if success is false
  "data": { ... }         // Optional return data
}
```

## 4. Implementing Capabilities

### 4.1 `package_manager`
Implement these commands to allow users to install packages via your plugin.

*   `check_installed`: Returns `{ "data": true/false }`
*   `install`: Installs the package.
*   `uninstall`: Removes the package.

**Config Usage:**
```yaml
apps:
  - id: my-package
    manager: my-awesome-plugin
```

### 4.2 `config_provider`
Implement the `apply` command to handle custom configuration sections.

*   `apply`: Receives the raw YAML section as `args`.

**Config Usage:**
```yaml
extensions:
  my-awesome-plugin:
    setting1: true
    theme: "dark"
```

## 5. Example: Python Plugin

`src/main.py`:
```python
import sys
import json

def main():
    # Read Input
    raw_input = sys.stdin.read()
    if not raw_input: return
    request = json.loads(raw_input)
    
    cmd = request.get("command")
    
    response = {
        "requestId": request.get("requestId"),
        "success": True,
        "changed": False
    }

    if cmd == "install":
        # Do logic...
        response["changed"] = True
        sys.stderr.write("Installing package...\n")
    
    print(json.dumps(response))

if __name__ == "__main__":
    main()
```
