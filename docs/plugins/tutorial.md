# Building Your First WinHome Plugin: A Step-by-Step Tutorial

This tutorial walks you through building a WinHome plugin from scratch. By the end, you'll have a working plugin with tests, ready to submit.

> **Prerequisites**: Basic knowledge of Python or JavaScript/TypeScript. No .NET or C# knowledge required.

---

## Table of Contents

1. [How Plugins Work](#1-how-plugins-work)
2. [Project Structure](#2-project-structure)
3. [The Manifest: `plugin.yaml`](#3-the-manifest-pluginyaml)
4. [The JSON IPC Protocol](#4-the-json-ipc-protocol)
5. [Hello World: Python Plugin](#5-hello-world-python-plugin)
6. [Hello World: TypeScript/JavaScript Plugin](#6-hello-world-typescriptjavascript-plugin)
7. [Concrete Example: Managing Windows App Settings](#7-concrete-example-managing-windows-app-settings)
8. [Testing with pytest](#8-testing-with-pytest)
9. [Submitting Your Plugin](#9-submitting-your-plugin)

---

## 1. How Plugins Work

WinHome uses a **process-based plugin architecture**. Instead of loading code directly, WinHome spawns your plugin as a separate child process and communicates with it through standard input/output streams.

This means:
- Plugins can be written in **any language** (Python, JavaScript, Go, etc.)
- A plugin crash **cannot** bring down WinHome
- No .NET knowledge is required

The communication flow is simple:

```
WinHome  ──(JSON request)──▶  Your Plugin (stdin)
WinHome  ◀──(JSON response)─  Your Plugin (stdout)
WinHome  ◀──(logs/debug)────  Your Plugin (stderr)
```

For a deeper look at the architecture, see [`docs/architecture/plugin_spec_v1.md`](../architecture/plugin_spec_v1.md).

---

## 2. Project Structure

Every plugin follows the same folder structure:

```
plugins/
└── my-plugin/
    ├── plugin.yaml          # Required: manifest file
    ├── src/
    │   └── plugin.py        # Your plugin's main entry point
    └── test/                # some existing plugins use "tests/" (plural) — either is fine
        └── test_plugin.py   # pytest tests
```

For a TypeScript plugin, `src/` would contain `index.ts` instead. You can refer to the existing [`obsidian`](../../plugins/obsidian) (Python) and [`vscode`](../../plugins/vscode) (TypeScript) plugins as real-world examples.

---

## 3. The Manifest: `plugin.yaml`

Every plugin needs a `plugin.yaml` in its root folder. This tells WinHome how to find and run your plugin.

```yaml
name: my-plugin
version: 0.1.0
type: python          # or "typescript"
main: src/plugin.py   # entry point relative to this folder
capabilities:
  - config_provider
```

| Field | Description |
|---|---|
| `name` | Unique plugin identifier |
| `version` | Semantic version of your plugin |
| `type` | Runtime type: `python` or `typescript` |
| `main` | Path to the entry point file |
| `capabilities` | What the plugin can do (use `config_provider` for now) |

---

## 4. The JSON IPC Protocol

When WinHome runs your plugin, it sends a single-line JSON object to your plugin's **stdin**. Your plugin must respond with a single-line JSON object to **stdout**.

### Request (WinHome → Plugin)

```json
{
  "requestId": "550e8400-e29b-41d4-a716-446655440000",
  "command": "apply",
  "args": {
    "setting1": "value"
  },
  "context": {
    "osVersion": "10.0.19045",
    "isAdmin": true,
    "dryRun": false
  }
}
```

| Field | Description |
|---|---|
| `requestId` | Unique ID — your response must echo this back |
| `command` | What to do — currently `apply` is the primary command used by all plugins |
| `args` | Your plugin's config from the user's `config.yaml` (referred to as `config` in the protocol spec) |
| `context` | System info provided by WinHome, including `dryRun`, `osVersion`, and `isAdmin` |

> **Note**: The protocol spec (`plugin_spec_v1.md`) refers to this field as `config`. The existing plugins (obsidian, vscode) use `args`. Follow the existing plugins when building your own.

### Response (Plugin → WinHome)

```json
{
  "requestId": "550e8400-e29b-41d4-a716-446655440000",
  "success": true,
  "changed": false,
  "error": null,
  "data": {
    "status": "nothing to do"
  }
}
```

| Field | Description |
|---|---|
| `requestId` | Must match the request's `requestId` |
| `success` | `true` if the plugin ran without errors |
| `changed` | `true` if the plugin modified system state |
| `error` | Error message string, or `null` on success |
| `data` | Optional result data |

> **Important**: Write all logs and debug messages to **stderr**, never stdout. WinHome captures stderr and pipes it to the main application log.

---

## 5. Hello World: Python Plugin

Create `plugins/hello-world/src/plugin.py`:

```python
import sys
import json

def main():
    # 1. Read the request from stdin
    raw = sys.stdin.read()
    request = json.loads(raw)

    request_id = request.get("requestId")
    command = request.get("command")
    context = request.get("context", {})
    dry_run = context.get("dryRun", False)

    # Log to stderr — WinHome captures this
    sys.stderr.write(f"[hello-world] Received command: {command}\n")

    # 2. Build the response
    response = {
        "requestId": request_id,
        "success": True,
        "changed": False,
        "error": None,
        "data": {}
    }

    if command == "apply":
        if dry_run:
            sys.stderr.write("[hello-world] Dry run — no changes made.\n")
        else:
            sys.stderr.write("[hello-world] Applying changes...\n")
            response["changed"] = True
            response["data"] = {"status": "Hello from my first WinHome plugin!"}
    else:
        response["success"] = False
        response["error"] = f"Unknown command: {command}"

    # 3. Write response to stdout
    print(json.dumps(response))

if __name__ == "__main__":
    main()
```

And the manifest at `plugins/hello-world/plugin.yaml`:

```yaml
name: hello-world
version: 0.1.0
type: python
main: src/plugin.py
capabilities:
  - config_provider
```

---

## 6. Hello World: TypeScript/JavaScript Plugin

Create `plugins/hello-world-js/src/index.ts` (or `main.js` for plain JavaScript):

```javascript
let inputData = "";

// 1. Read the request from stdin
process.stdin.on("data", (chunk) => {
    inputData += chunk;
});

process.stdin.on("end", () => {
    const request = JSON.parse(inputData);
    const { requestId, command, context = {} } = request;
    const dryRun = context.dryRun ?? false;

    // Log to stderr — WinHome captures this
    process.stderr.write(`[hello-world-js] Received command: ${command}\n`);

    // 2. Build the response
    const response = {
        requestId,
        success: true,
        changed: false,
        error: null,
        data: {}
    };

    if (command === "apply") {
        if (dryRun) {
            process.stderr.write("[hello-world-js] Dry run — no changes made.\n");
        } else {
            process.stderr.write("[hello-world-js] Applying changes...\n");
            response.changed = true;
            response.data = { status: "Hello from my first WinHome plugin!" };
        }
    } else {
        response.success = false;
        response.error = `Unknown command: ${command}`;
    }

    // 3. Write response to stdout
    process.stdout.write(JSON.stringify(response) + "\n");
});
```

And the manifest at `plugins/hello-world-js/plugin.yaml`:

```yaml
name: hello-world-js
version: 0.1.0
type: typescript
main: src/index.ts
capabilities:
  - config_provider
```

> **Note**: The example above follows the same pattern as the existing [`vscode`](../../plugins/vscode) plugin. The code is written in plain JavaScript syntax for readability — if you want full TypeScript, add type annotations to your file. Check `plugins/vscode/plugin.yaml` to confirm the exact `type` value to use in your manifest.

---

## 7. Concrete Example: Managing Windows App Settings

The Hello World plugins above show the basic structure. Now let's build something real that you could actually use — a plugin that reads a user's desired settings from `config.yaml` and writes them to a Windows application's JSON config file. This is exactly the same pattern used by the [`obsidian`](../../plugins/obsidian) plugin, which manages Obsidian's settings files the same way.

We'll use this concrete example for the testing section as well.

Create `plugins/app-settings/src/plugin.py`:

```python
import sys
import json
import os

def log(msg):
    sys.stderr.write(f"[app-settings] {msg}\n")
    sys.stderr.flush()

def read_settings(path):
    if not os.path.exists(path):
        return {}
    with open(path, "r", encoding="utf-8") as f:
        return json.load(f)

def write_settings(path, data):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "w", encoding="utf-8") as f:
        json.dump(data, f, indent=2)

def apply(args, dry_run):
    settings_path = args.get("settingsPath")
    desired = args.get("settings", {})

    if not settings_path:
        return {"success": False, "error": "settingsPath is required"}

    current = read_settings(settings_path)

    # Check what actually needs to change
    changed_keys = {k: v for k, v in desired.items() if current.get(k) != v}

    if not changed_keys:
        log("Settings already up to date.")
        return {"success": True, "changed": False}

    if dry_run:
        log(f"Would update: {list(changed_keys.keys())}")
        return {"success": True, "changed": False}

    current.update(changed_keys)
    write_settings(settings_path, current)
    log(f"Updated settings: {list(changed_keys.keys())}")
    return {"success": True, "changed": True}

def main():
    request = json.loads(sys.stdin.read())

    request_id = request.get("requestId")
    command = request.get("command")
    args = request.get("args", {})
    context = request.get("context", {})
    dry_run = context.get("dryRun", False)

    response = {"requestId": request_id, "success": False, "changed": False, "error": None}

    try:
        if command == "apply":
            result = apply(args, dry_run)
            response.update(result)
        else:
            response["error"] = f"Unknown command: {command}"
    except Exception as e:
        response["error"] = str(e)

    print(json.dumps(response))

if __name__ == "__main__":
    main()
```

A user would configure this in their `config.yaml` like:

```yaml
app-settings:
  settingsPath: "C:/Users/John/AppData/Roaming/MyApp/settings.json"
  settings:
    theme: "dark"
    fontSize: 14
    autoSave: true
```

---

## 8. Testing with pytest

Now let's write tests for the `app-settings` plugin we built in Section 7. WinHome plugins are tested by running them as subprocesses and asserting their JSON responses — the same pattern used in the [`obsidian` tests](../../plugins/obsidian/test/test_obsidian.py). This approach works for any plugin regardless of language.

Create `plugins/app-settings/test/test_plugin.py`:

```python
import subprocess
import json
import os
import sys
import tempfile

# Path to your plugin
PLUGIN = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "src", "plugin.py"))

def run_plugin(payload):
    """Helper to run the plugin and return the parsed response."""
    result = subprocess.run(
        [sys.executable, PLUGIN],
        input=json.dumps(payload),
        capture_output=True,
        text=True
    )
    return json.loads(result.stdout.strip())


def test_apply_settings():
    """Settings should be written to the file."""
    with tempfile.TemporaryDirectory() as tmp:
        settings_path = os.path.join(tmp, "app", "settings.json")
        res = run_plugin({
            "requestId": "1",
            "command": "apply",
            "args": {
                "settingsPath": settings_path,
                "settings": {"theme": "dark", "fontSize": 14}
            },
            "context": {"dryRun": False}
        })
        assert res["success"]
        assert res["changed"]

        written = json.loads(open(settings_path).read())
        assert written["theme"] == "dark"
        assert written["fontSize"] == 14


def test_idempotent():
    """Running the same config twice should not report changed the second time."""
    with tempfile.TemporaryDirectory() as tmp:
        settings_path = os.path.join(tmp, "app", "settings.json")
        payload = {
            "requestId": "2",
            "command": "apply",
            "args": {"settingsPath": settings_path, "settings": {"theme": "dark"}},
            "context": {"dryRun": False}
        }
        run_plugin(payload)
        res = run_plugin(payload)
        assert res["success"]
        assert not res["changed"]


def test_dry_run():
    """Dry run should not write any files."""
    with tempfile.TemporaryDirectory() as tmp:
        settings_path = os.path.join(tmp, "app", "settings.json")
        res = run_plugin({
            "requestId": "3",
            "command": "apply",
            "args": {"settingsPath": settings_path, "settings": {"theme": "dark"}},
            "context": {"dryRun": True}
        })
        assert res["success"]
        assert not res["changed"]
        assert not os.path.exists(settings_path)


def test_unknown_command():
    """Unknown commands should return success: false with an error."""
    res = run_plugin({
        "requestId": "4",
        "command": "explode",
        "args": {},
        "context": {"dryRun": False}
    })
    assert not res["success"]
    assert "error" in res


if __name__ == "__main__":
    test_apply_settings()
    test_idempotent()
    test_dry_run()
    test_unknown_command()
    print("\nAll tests passed.")
```

Run tests with:

```bash
pytest plugins/app-settings/test/
```

Always write tests for at least these four cases:
- **Normal apply** — changes are made correctly
- **Idempotent** — running twice doesn't report changed the second time
- **Dry run** — no files or system state are modified
- **Unknown command** — fails gracefully with an error message

---

## 9. Submitting Your Plugin

1. **Place your plugin** in the `plugins/` directory following the structure above.

2. **Name your executable** following the convention from the spec:
   ```
   winhome-provider-<your-plugin-name>
   ```

3. **Ensure your plugin handles** at minimum the `apply` command and respects `dryRun`.

4. **Run your tests** and make sure they all pass:
   ```bash
   pytest plugins/your-plugin/test/
   ```

5. **Open a Pull Request** following the guidelines in [`CONTRIBUTING.md`](../../CONTRIBUTING.md).

---

## Summary

| What | Where |
|---|---|
| Plugin manifest | `plugin.yaml` in plugin root |
| Entry point | `src/plugin.py` or `src/index.ts` |
| Tests | `test/test_<name>.py` |
| Architecture spec | `docs/architecture/plugin_spec_v1.md` |
| Real Python example | `plugins/obsidian/` |
| Real TypeScript example | `plugins/vscode/` |

You're ready to build. 
