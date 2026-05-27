import json
import os
import sys
import shutil
import tempfile
import uuid


SETTING_FILE = "settings.json"


def log(msg):
    sys.stderr.write(f"[joplin-plugin] {msg}\n")
    sys.stderr.flush()


def get_config_path():
    appdata = os.getenv("APPDATA")

    if not appdata:
        raise Exception("APPDATA environment variable not found")

    config_dir = os.path.join(appdata, "joplin-desktop")
    os.makedirs(config_dir, exist_ok=True)

    return os.path.join(config_dir, SETTING_FILE)


def read_json(file_path: str) -> dict:
    if not os.path.exists(file_path):
        return {}

    try:
        with open(file_path, "r", encoding="utf-8") as f:
            return json.load(f)
    except Exception as e:
        # Back up the corrupted file with a UUID suffix before starting fresh
        backup_path = file_path + f".{uuid.uuid4().hex}.bak"
        try:
            shutil.copy2(file_path, backup_path)
            log(f"Warning: corrupted config backed up to {backup_path}: {e}")
        except Exception as backup_err:
            log(f"Warning: could not back up corrupted config: {backup_err}")
        return {}


def write_json(file_path: str, data: dict) -> None:
    os.makedirs(os.path.dirname(file_path), exist_ok=True)

    # Atomic write: write to a temp file then replace to prevent corruption
    dir_name = os.path.dirname(file_path)
    fd, tmp_path = tempfile.mkstemp(dir=dir_name, suffix=".tmp")
    try:
        with os.fdopen(fd, "w", encoding="utf-8") as f:
            json.dump(data, f, indent=2)
        os.replace(tmp_path, file_path)
    except Exception:
        # Clean up temp file if something went wrong
        try:
            os.unlink(tmp_path)
        except OSError:
            pass
        raise


def merge_settings(target: dict, source: dict) -> bool:
    changed = False

    for key, value in source.items():
        if key not in target or target[key] != value:
            target[key] = value
            changed = True

    return changed


def check_installed(args: dict, request_id: str) -> dict:
    installed = False

    # Check if joplin-desktop.exe or joplin.exe is in PATH
    if shutil.which("joplin-desktop.exe") or shutil.which("joplin.exe"):
        installed = True

    return {
        "requestId": request_id,
        "success": True,
        "changed": False,
        "data": installed,
    }


def apply_config(args: dict, context: dict, request_id: str) -> dict:
    dry_run = context.get("dryRun", False)
    settings = args.get("settings", {})

    try:
        config_path = get_config_path()

        current_config = read_json(config_path)

        changed = merge_settings(current_config, settings)

        if not changed:
            return {
                "requestId": request_id,
                "success": True,
                "changed": False,
                "data": None,
            }

        if dry_run:
            log(f"Would update {config_path} with: {json.dumps(settings)}")

            return {
                "requestId": request_id,
                "success": True,
                "changed": True,  # report true — changes would be made
                "data": None,
            }

        write_json(config_path, current_config)

        log(f"Updated Joplin config: {config_path}")

        return {
            "requestId": request_id,
            "success": True,
            "changed": True,
            "data": None,
        }

    except Exception as e:
        log(f"Failed to apply config: {e}")

        return {
            "requestId": request_id,
            "success": False,
            "changed": False,
            "data": None,
            "error": str(e),
        }


def main():
    input_data = sys.stdin.read()

    if not input_data:
        response = {
            "requestId": "unknown",
            "success": False,
            "changed": False,
            "data": None,
            "error": "Empty input received from host",
        }
        sys.stdout.write(json.dumps(response) + "\n")
        sys.stdout.flush()
        return

    try:
        request = json.loads(input_data)
    except Exception as e:
        response = {
            "requestId": "unknown",
            "success": False,
            "changed": False,
            "data": None,
            "error": f"Failed to parse request: {e}",
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
        "data": None,
    }

    try:
        if command == "check_installed":
            response = check_installed(args, request_id)

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
