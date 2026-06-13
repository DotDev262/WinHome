import json
import os
import sys
import tempfile


def get_settings_path():
    appdata = os.environ.get("APPDATA", "")
    if not appdata:
        return None
    return os.path.join(appdata, "FlowLauncher", "Settings", "Settings.json")


def log(msg):
    sys.stderr.write(f"[flow-launcher-plugin] {msg}\n")


def read_settings(path):
    if not os.path.exists(path):
        return {}, None
    try:
        with open(path, "r", encoding="utf-8") as f:
            return json.load(f), None
    except Exception as e:
        return None, f"Error reading {path}: {e}"


def write_settings(path, data):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    fd, temp_path = tempfile.mkstemp(dir=os.path.dirname(path), suffix=".json")
    with os.fdopen(fd, "w", encoding="utf-8") as tmp:
        json.dump(data, tmp, indent=4)
        tmp.write("\n")
    os.replace(temp_path, path)


def merge_dict(target, updates):
    changed = False
    for key, value in updates.items():
        if isinstance(value, dict) and isinstance(target.get(key), dict):
            if merge_dict(target[key], value):
                changed = True
        else:
            if target.get(key) != value:
                target[key] = value
                changed = True
    return changed


def apply_config(args):
    dry_run = args.get("dryRun", False)
    settings = args.get("settings", {})
    path = get_settings_path()

    if not path:
        return {"changed": False, "error": "APPDATA is not set."}

    current, error = read_settings(path)
    if error:
        log(error)
        return {"changed": False, "error": error}

    if not isinstance(settings, dict):
        return {"changed": False, "error": "Configuration must be a dictionary."}

    changed = merge_dict(current, settings)

    if not changed:
        log("Settings already up to date")
        return {"changed": False}

    if dry_run:
        log(f"Would update Flow Launcher settings at {path}")
        return {"changed": True}

    try:
        write_settings(path, current)
        log("Updated Flow Launcher settings")
        return {"changed": True}
    except Exception as e:
        error_msg = f"Error writing settings: {e}"
        log(error_msg)
        return {"changed": False, "error": error_msg}


def check_installed(args):
    path = get_settings_path()
    if not path:
        return False
    return os.path.exists(path)


def main():
    input_data = sys.stdin.read()
    if not input_data:
        sys.stdout.write(json.dumps({"requestId": "unknown", "error": "No input received"}) + "\n")
        return

    try:
        request = json.loads(input_data)
    except Exception as e:
        log(f"Failed to parse request: {e}")
        sys.stdout.write(json.dumps({"requestId": "unknown", "error": "Invalid JSON"}) + "\n")
        return

    command = request.get("command")
    args = request.get("args", {})
    request_id = request.get("requestId") or "unknown"

    if command == "apply":
        result = apply_config(args)
        response = {"requestId": request_id}
        response.update(result)
    elif command == "check_installed":
        installed = check_installed(args)
        response = {"requestId": request_id, "installed": installed}
    else:
        response = {"requestId": request_id, "error": f"Unknown command: {command}"}

    sys.stdout.write(json.dumps(response) + "\n")


if __name__ == "__main__":
    main()
