import json
import os
import shutil
import sys
import tempfile
import uuid


PREFERENCES_FILE = "Preferences.sublime-settings"
SUBLIME_APPDATA_DIR = os.path.join("Sublime Text", "Packages", "User")
COMMON_INSTALL_PATHS = [
    os.path.join("Sublime Text", "sublime_text.exe"),
    os.path.join("Sublime Text 3", "sublime_text.exe"),
    os.path.join("Sublime Text", "subl.exe"),
    os.path.join("Sublime Text 3", "subl.exe"),
]


def log(msg):
    sys.stderr.write(f"[sublime-text-plugin] {msg}\n")
    sys.stderr.flush()


def get_preferences_path():
    appdata = os.getenv("APPDATA")
    if not appdata:
        raise Exception("APPDATA environment variable not found")

    config_dir = os.path.join(appdata, SUBLIME_APPDATA_DIR)
    return os.path.join(config_dir, PREFERENCES_FILE)


def read_json(file_path: str) -> dict:
    if not os.path.exists(file_path):
        return {}

    try:
        with open(file_path, "r", encoding="utf-8") as f:
            data = json.load(f)
            return data if isinstance(data, dict) else {}
    except json.JSONDecodeError as e:
        backup_path = f"{file_path}.{uuid.uuid4()}.bak"
        shutil.copy2(file_path, backup_path)
        log(f"Backed up corrupt preferences file to {backup_path}: {e}")
        return {}


def write_json(file_path: str, data: dict) -> None:
    os.makedirs(os.path.dirname(file_path), exist_ok=True)
    temp_path = None

    try:
        fd, temp_path = tempfile.mkstemp(
            prefix="sublime-text-",
            dir=os.path.dirname(file_path),
        )

        with os.fdopen(fd, "w", encoding="utf-8") as f:
            json.dump(data, f, indent=2)
            f.write("\n")

        os.replace(temp_path, file_path)
    finally:
        if temp_path and os.path.exists(temp_path):
            os.remove(temp_path)


def deep_merge(target: dict, source: dict) -> bool:
    changed = False

    for key, value in source.items():
        if isinstance(value, dict) and isinstance(target.get(key), dict):
            if deep_merge(target[key], value):
                changed = True
            continue

        if key not in target or target[key] != value:
            target[key] = value
            changed = True

    return changed


def executable_exists_in_common_location() -> bool:
    program_files = [
        os.getenv("ProgramFiles"),
        os.getenv("ProgramFiles(x86)"),
        os.getenv("LOCALAPPDATA"),
    ]

    for root in program_files:
        if not root:
            continue

        for relative_path in COMMON_INSTALL_PATHS:
            if os.path.exists(os.path.join(root, relative_path)):
                return True

    return False


def check_installed(_args: dict, request_id: str) -> dict:
    installed = (
        shutil.which("sublime_text.exe") is not None
        or shutil.which("sublime_text") is not None
        or shutil.which("subl.exe") is not None
        or shutil.which("subl") is not None
        or executable_exists_in_common_location()
    )

    return {
        "requestId": request_id,
        "success": True,
        "changed": False,
        "error": None,
        "data": installed,
    }


def apply_config(args: dict, request_id: str, dry_run: bool) -> dict:
    try:
        config_path = get_preferences_path()
        current_config = read_json(config_path)
        desired_config = args.get("settings", {})

        if not isinstance(desired_config, dict):
            return {
                "requestId": request_id,
                "success": False,
                "changed": False,
                "error": "apply args must be a JSON object",
                "data": {},
            }

        changed = deep_merge(current_config, desired_config)

        if not changed:
            return {
                "requestId": request_id,
                "success": True,
                "changed": False,
                "error": None,
                "data": {},
            }

        if dry_run:
            log(
                f"Dry run: would update {config_path} with "
                f"{json.dumps(desired_config, sort_keys=True)}"
            )
            return {
                "requestId": request_id,
                "success": True,
                "changed": True,
                "error": None,
                "data": {"dryRun": True, "path": config_path},
            }

        write_json(config_path, current_config)
        log(f"Updated Sublime Text preferences: {config_path}")

        return {
            "requestId": request_id,
            "success": True,
            "changed": True,
            "error": None,
            "data": {"path": config_path},
        }

    except Exception as e:
        log(f"Failed to apply config: {e}")
        return {
            "requestId": request_id,
            "success": False,
            "changed": False,
            "error": str(e),
            "data": {},
        }


def build_response(request: dict) -> dict:
    request_id = request.get("requestId", "unknown")
    command = request.get("command")
    args = request.get("args", {})
    context = request.get("context", {})
    dry_run = context.get("dryRun", False)

    if command == "check_installed":
        return check_installed(args, request_id)

    if command == "apply":
        return apply_config(args, request_id, dry_run)

    return {
        "requestId": request_id,
        "success": False,
        "changed": False,
        "error": f"Unknown command: {command}",
        "data": {},
    }


def main():
    input_data = sys.stdin.read()
    if not input_data:
        response = {
            "requestId": "unknown",
            "success": False,
            "changed": False,
            "error": "Empty stdin",
            "data": {},
        }
        sys.stdout.write(json.dumps(response) + "\n")
        sys.stdout.flush()
        return

    try:
        request = json.loads(input_data)
        response = build_response(request)
    except Exception as e:
        log(f"Internal Script Error: {e}")
        response = {
            "requestId": "unknown",
            "success": False,
            "changed": False,
            "error": f"Internal Script Error: {str(e)}",
            "data": {},
        }

    sys.stdout.write(json.dumps(response) + "\n")
    sys.stdout.flush()


if __name__ == "__main__":
    main()
