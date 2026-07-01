import sys
import os
import json
import tempfile

def deep_merge(base, update):
    """
    Safely executes deep recursive associative data structure mapping loops.
    """
    if not isinstance(base, dict) or not isinstance(update, dict):
        return update
    for key, val in update.items():
        if key in base and isinstance(base[key], dict) and isinstance(val, dict):
            base[key] = deep_merge(base[key], val)
        else:
            base[key] = val
    return base

def check_installed() -> bool:
    """
    Decoupled utility to determine installation state profiles cleanly.
    """
    appdata = os.environ.get("APPDATA", "")
    config_path = os.path.join(appdata, "GitHub Desktop", "config.json") if appdata else ""
    return bool(config_path and os.path.exists(config_path))

def main():
    raw_input = sys.stdin.read().strip()
    if not raw_input:
        print(json.dumps({
            "requestId": "unknown",
            "error": "Empty stdin context payload received"
        }))
        return

    try:
        request = json.loads(raw_input)
    except json.JSONDecodeError:
        print(json.dumps({
            "requestId": "unknown",
            "error": "Invalid JSON format payload structure"
        }))
        return

    request_id = request.get("requestId", "unknown")
    command = request.get("command", "")
    args = request.get("args", {})

    if command == "check_installed":
        installed_status = check_installed()
        print(json.dumps({
            "requestId": request_id,
            "installed": installed_status
        }))
        return

    elif command == "apply":
        settings = args.get("settings", {})
        dry_run = args.get("dryRun", False)

        appdata = os.environ.get("APPDATA", "")
        if not appdata:
            print(json.dumps({
                "requestId": request_id,
                "error": "APPDATA environment variable missing"
            }))
            return

        config_dir = os.path.join(appdata, "GitHub Desktop")
        config_path = os.path.join(config_dir, "config.json")

        current_config = {}
        if os.path.exists(config_path):
            try:
                with open(config_path, "r", encoding="utf-8") as f:
                    current_config = json.load(f)
            except Exception:
                current_config = {}

        updated_config = deep_merge(dict(current_config), settings)
        changes_would_occur = current_config != updated_config

        if dry_run:
            print(json.dumps({
                "requestId": request_id,
                "changed": changes_would_occur
            }))
            return

        if not os.path.exists(config_dir):
            os.makedirs(config_dir, exist_ok=True)
        try:
            fd, temp_path = tempfile.mkstemp(dir=config_dir, prefix="config_", suffix=".json")
            with os.fdopen(fd, "w", encoding="utf-8") as f:
                json.dump(updated_config, f, indent=2)
            os.replace(temp_path, config_path)
        except Exception as e:
            print(json.dumps({
                "requestId": request_id,
                "error": f"Atomic write operation exception: {str(e)}"
            }))
            return

        print(json.dumps({
            "requestId": request_id,
            "changed": changes_would_occur
        }))
    else:
        print(json.dumps({
            "requestId": request_id,
            "error": f"Unknown execution command structural parameter: {command}"
        }))

if __name__ == "__main__":
    main()

