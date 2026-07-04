import json
import os
import shutil
import sys
import tempfile

# from pathlib import Path


SETTING_FILE = "Preferences.sublime-settings"


def log(msg):
    sys.stderr.write(f"[sublime-text-plugin] {msg}\n")
    sys.stderr.flush()


def get_config_path():
    appdata = os.getenv("APPDATA")

    if not appdata:
        raise Exception("APPDATA environment variable not found")

    config_dir = os.path.join(
        appdata,
        "Sublime Text",
        "Packages",
        "User",
    )

    os.makedirs(config_dir, exist_ok=True)

    return os.path.join(config_dir, SETTING_FILE)


def read_json(file_path: str) -> dict:
    if not os.path.exists(file_path):
        return {}

    try:
        with open(file_path, "r", encoding="utf-8") as f:
            return json.load(f)
    except Exception as e:
        log(f"Warning: could not parse {file_path}: {e}")
        return {}


def write_json(file_path: str, data) -> None:
    os.makedirs(os.path.dirname(file_path), exist_ok=True)

    fd, temp_path = tempfile.mkstemp(
        dir=os.path.dirname(file_path),
        suffix=".tmp",
    )

    try:
        with os.fdopen(fd, "w", encoding="utf-8") as f:
            json.dump(data, f, indent=2)

        os.replace(temp_path, file_path)

    finally:
        if os.path.exists(temp_path):
            os.remove(temp_path)


def merge_settings(target: dict, source: dict) -> bool:
    changed = False

    for key, value in source.items():
        if key not in target or target[key] != value:
            target[key] = value
            changed = True

    return changed


def check_installed() -> bool:
    return shutil.which("subl.exe") is not None or shutil.which("sublime_text.exe") is not None


def apply_config(args: dict, context: dict, request_id: str) -> dict:
    dry_run = args.get("dryRun", False)
    settings = args.get("settings", {})

    try:
        config_path = get_config_path()
        current_config = read_json(config_path)

        changed = merge_settings(current_config, settings)

        if not changed:
            return {
                "requestId": request_id,
                "changed": False,
            }

        if dry_run:
            log(f"Would update {config_path} with: {json.dumps(settings)}")
            return {
                "requestId": request_id,
                "changed": True,
            }

        write_json(config_path, current_config)

        log(f"Updated Sublime Text config: {config_path}")

        return {
            "requestId": request_id,
            "changed": True,
        }

    except Exception as e:
        log(f"Failed to apply config: {e}")
        return {
            "requestId": request_id,
            "changed": False,
            "error": str(e),
        }


def main():
    input_data = sys.stdin.read()

    if not input_data:
        response = {
            "requestId": "unknown",
            "error": "Empty request",
        }
        sys.stdout.write(json.dumps(response) + "\n")
        sys.stdout.flush()
        return

    try:
        request = json.loads(input_data)
    except Exception as e:
        log(f"Failed to parse request: {e}")
        response = {
            "requestId": "unknown",
            "error": f"Failed to parse request: {str(e)}",
        }
        sys.stdout.write(json.dumps(response) + "\n")
        sys.stdout.flush()
        return

    request_id = request.get("requestId") or "unknown"
    command = request.get("command")
    args = request.get("args", {})
    context = request.get("context", {})

    response = {
        "requestId": request_id,
    }

    try:
        if command == "check_installed":
            response = {
                "requestId": request_id,
                "installed": check_installed(),
            }

        elif command == "apply":
            response = apply_config(args, context, request_id)

        else:
            response["error"] = f"Unknown command: {command}"

    except Exception as fatal_err:
        response["error"] = f"Internal Script Error: {str(fatal_err)}"

    sys.stdout.write(json.dumps(response) + "\n")
    sys.stdout.flush()


if __name__ == "__main__":
    main()
