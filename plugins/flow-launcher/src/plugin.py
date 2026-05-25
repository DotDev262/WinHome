import sys
import json
import os
import shutil

def log(msg):
    sys.stderr.write(f"[flow-launcher-plugin] {msg}\n")
    sys.stderr.flush()

def get_settings_path():
    """Resolves the path to the Flow Launcher settings file."""
    app_data = os.getenv("APPDATA")
    if app_data:
        return os.path.join(app_data, "FlowLauncher", "Settings.json")
    return None

def read_json(file_path):
    """Reads the JSON config if it exists."""
    if not os.path.exists(file_path):
        return {}
    try:
        with open(file_path, "r", encoding="utf-8") as f:
            return json.load(f)
    except Exception as e:
        log(f"Warning: could not parse {file_path}: {e}")
        return {}

def write_json(file_path, data):
    """Writes the JSON config atomically and handles missing directories."""
    os.makedirs(os.path.dirname(file_path), exist_ok=True)
    temp_path = file_path + ".tmp"
    try:
        with open(temp_path, "w", encoding="utf-8") as f:
            json.dump(data, f, indent=2)
        os.replace(temp_path, file_path)
    except Exception:
        if os.path.exists(temp_path):
            os.remove(temp_path)
        raise

def merge_settings(target, source):
    """Deep-merges new config values into existing config."""
    changed = False
    for key, value in source.items():
        if isinstance(value, dict) and isinstance(target.get(key), dict):
            if merge_settings(target[key], value):
                changed = True
        elif key not in target or target[key] != value:
            target[key] = value
            changed = True
    return changed

def check_installed(args, request_id):
    """Checks if Flow Launcher is installed by looking for its AppData directory."""
    app_data = os.getenv("APPDATA")
    installed = False
    if app_data:
        installed = os.path.isdir(os.path.join(app_data, "FlowLauncher"))
        
    return {
        "requestId": request_id,
        "success": True,
        "changed": False,
        "data": {"installed": installed},
    }

def apply_config(args, context, request_id):
    """Applies new config properties to Settings.json."""
    dry_run = context.get("dryRun", False)

    try:
        settings_path = get_settings_path()
        if not settings_path:
            return {
                "requestId": request_id,
                "success": False,
                "changed": False,
                "error": "Could not determine APPDATA directory.",
            }

        if not os.path.exists(settings_path):
            log(f"No existing Settings.json found. Will create at: {settings_path}")

        current = read_json(settings_path)
        changed = merge_settings(current, args)

        if not changed:
            return {
                "requestId": request_id,
                "success": True,
                "changed": False,
            }

        if dry_run:
            log(f"Would update {settings_path} with: {json.dumps(args)}")
            return {
                "requestId": request_id,
                "success": True,
                "changed": True,
            }

        write_json(settings_path, current)
        log(f"Updated Flow Launcher config: {settings_path}")
        
        return {
            "requestId": request_id,
            "success": True,
            "changed": True,
        }

    except Exception as e:
        log(f"Failed to apply config: {e}")
        return {
            "requestId": request_id,
            "success": False,
            "changed": False,
            "error": str(e),
        }

def main():
    """Main JSON-over-stdio communication loop."""
    input_data = sys.stdin.read()
    if not input_data:
        return

    try:
        request = json.loads(input_data)
    except Exception as e:
        log(f"Failed to parse request: {e}")
        response = {
            "requestId": "unknown", 
            "success": False, 
            "changed": False, 
            "error": f"Failed to parse request: {str(e)}"
        }
        sys.stdout.write(json.dumps(response) + "\n")
        sys.stdout.flush()
        return

    request_id = request.get("requestId", "unknown")
    command = request.get("command")
    args = request.get("args", {})
    context = request.get("context", {})

    response = {
        "requestId": request_id,
        "success": False,
        "changed": False,
    }

    try:
        if command == "check_installed":
            response = check_installed(args, request_id)
        elif command == "apply":
            response = apply_config(args, context, request_id)
        else:
            response["error"] = f"Unknown command: {command}"

    except Exception as e:
        response["error"] = f"Internal Script Error: {str(e)}"

    # Ensure valid JSON on single line is written to stdout
    sys.stdout.write(json.dumps(response) + "\n")
    sys.stdout.flush()

if __name__ == "__main__":
    main()