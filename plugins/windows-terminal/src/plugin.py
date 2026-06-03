import json
import os
import shutil
import sys


def log(msg):
    sys.stderr.write(f"[windows-terminal-plugin] {msg}\n")
    sys.stderr.flush()


def get_settings_paths():
    local_app_data = os.getenv("LOCALAPPDATA")
    user_profile = os.getenv("USERPROFILE")
    paths = []
    if local_app_data:
        paths += [
            os.path.join(
                local_app_data,
                "Packages",
                "Microsoft.WindowsTerminal_8wekyb3d8bbwe",
                "LocalState",
                "settings.json",
            ),
            os.path.join(
                local_app_data,
                "Packages",
                "Microsoft.WindowsTerminalPreview_8wekyb3d8bbwe",
                "LocalState",
                "settings.json",
            ),
            os.path.join(
                local_app_data,
                "Packages",
                "Microsoft.WindowsTerminalDev_8wekyb3d8bbwe",
                "LocalState",
                "settings.json",
            ),
        ]
    if user_profile:
        paths.append(os.path.join(user_profile, ".config", "wt", "settings.json"))
    return paths


def get_active_settings_path():
    for path in get_settings_paths():
        if os.path.exists(path):
            return path
    return None


def read_json(file_path):
    if not os.path.exists(file_path):
        return {}
    try:
        with open(file_path, "r", encoding="utf-8") as f:
            return json.load(f)
    except Exception as e:
        log(f"Warning: could not parse {file_path}: {e}")
        return {}


def write_json(file_path, data):
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
    installed = shutil.which("wt") is not None or get_active_settings_path() is not None
    return {
        "requestId": request_id,
        "success": True,
        "changed": False,
        "data": {"installed": installed},
    }


def apply_config(args, context, request_id):
    dry_run = context.get("dryRun", False)

    try:
        settings_path = get_active_settings_path()

        if not settings_path:
            stable = get_settings_paths()[0]
            settings_path = stable
            log(f"No existing settings.json found. Will create at: {settings_path}")

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
        log(f"Updated Windows Terminal config: {settings_path}")
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
    input_data = sys.stdin.read()
    if not input_data:
        return

    try:
        request = json.loads(input_data)
    except Exception as e:
        log(f"Failed to parse request: {e}")
        sys.exit(1)

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

    sys.stdout.write(json.dumps(response) + "\n")
    sys.stdout.flush()


if __name__ == "__main__":
    main()
