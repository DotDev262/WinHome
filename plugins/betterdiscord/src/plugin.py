import json
import os
import sys
import tempfile


SETTINGS_PATH = os.path.expandvars(
    r"%APPDATA%\BetterDiscord\data\settings.json"
)


def log(msg):
    sys.stderr.write(f"[betterdiscord-plugin] {msg}\n")
    sys.stderr.flush()


def read_json(file_path: str) -> dict:
    if not os.path.exists(file_path):
        return {}

    try:
        with open(file_path, "r", encoding="utf-8") as f:
            return json.load(f)
    except Exception as e:
        log(f"Failed to read json: {e}")

    try:
        import shutil
        import uuid

        backup_path = f"{file_path}.{uuid.uuid4()}.bak"
        shutil.copy(file_path, backup_path)

        log(f"Corrupted file backed up to: {backup_path}")

    except Exception as backup_error:
        log(f"Backup failed: {backup_error}")

    return {}


def deep_merge(original: dict, updates: dict) -> dict:
    for key, value in updates.items():
        if (
            key in original
            and isinstance(original[key], dict)
            and isinstance(value, dict)
        ):
            deep_merge(original[key], value)
        else:
            original[key] = value

    return original


def write_json_atomic(file_path: str, data: dict):
    os.makedirs(os.path.dirname(file_path), exist_ok=True)

    fd, temp_path = tempfile.mkstemp()

    try:
        with os.fdopen(fd, "w", encoding="utf-8") as tmp:
            json.dump(data, tmp, indent=2)
            tmp.write("\n")

        os.replace(temp_path, file_path)

    except Exception:
        if os.path.exists(temp_path):
            os.remove(temp_path)
        raise


def check_installed(args: dict, request_id: str) -> dict:
    install_path = os.path.expandvars(r"%APPDATA%\BetterDiscord")

    return {
        "requestId": request_id,
        "success": True,
        "changed": False,
        "data": os.path.exists(install_path),
    }


def apply_config(args: dict, context: dict, request_id: str) -> dict:
    settings = args.get("settings", {})

    if not isinstance(settings, dict):
        return {
            "requestId": request_id,
            "success": False,
            "changed": False,
            "data": None,
            "error": "settings must be an object"
        }

    dry_run = context.get("dryRun", False)

    current = read_json(SETTINGS_PATH)

    updated = json.loads(json.dumps(current))
    deep_merge(updated, settings)

    changed = current != updated

    if not changed:
        return {
            "requestId": request_id,
            "success": True,
            "changed": False,
            "data": None,
        }

    if dry_run:
        log("Dry run: settings would be updated")

        return {
            "requestId": request_id,
            "success": True,
            "changed": True,
            "data": None,
        }

    try:
        write_json_atomic(SETTINGS_PATH, updated)

        return {
            "requestId": request_id,
            "success": True,
            "changed": True,
            "data": None,
        }

    except Exception as e:
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
        print(json.dumps({
            "requestId": "unknown",
            "success": False,
            "changed": False,
            "data": None,
            "error": "No input received on stdin",
        }))

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
            "error": f"Failed to parse request: {str(e)}",
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

    except Exception as e:
        response["error"] = f"Internal Script Error: {str(e)}"

    sys.stdout.write(json.dumps(response) + "\n")
    sys.stdout.flush()


if __name__ == "__main__":
    main()
