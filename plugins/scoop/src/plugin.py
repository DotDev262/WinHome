import datetime
import json
import os
import shutil
import sys
import tempfile
import uuid


def log(msg):
    sys.stderr.write(f"[scoop-plugin] {msg}\n")
    sys.stderr.flush()


def get_config_path():
    # Scoop reads config from $env:XDG_CONFIG_HOME\scoop\config.json if set,
    # otherwise from %USERPROFILE%\.config\scoop\config.json.
    # Reference: https://github.com/ScoopInstaller/Scoop/blob/master/lib/core.ps1
    xdg_config = os.getenv("XDG_CONFIG_HOME")
    if xdg_config:
        return os.path.join(xdg_config, "scoop", "config.json")

    user_profile = os.getenv("USERPROFILE")
    if not user_profile:
        user_profile = os.path.expanduser("~")

    return os.path.join(user_profile, ".config", "scoop", "config.json")


def _backup_corrupt_config(file_path: str, reason: str):
    timestamp = datetime.datetime.now(datetime.timezone.utc).strftime("%Y%m%d%H%M%S")
    suffix = uuid.uuid4().hex[:8]
    backup_path = f"{file_path}.corrupted.{timestamp}.{suffix}"
    log(f"Config read failed ({reason}). Backing up to {backup_path} and starting fresh.")
    try:
        shutil.move(file_path, backup_path)
    except Exception as backup_e:
        log(f"Failed to backup corrupted config: {backup_e}")


def read_json(file_path: str) -> dict:
    if not os.path.exists(file_path):
        return {}

    try:
        with open(file_path, "r", encoding="utf-8") as f:
            data = json.load(f)
            return data if isinstance(data, dict) else {}
    except json.JSONDecodeError as e:
        _backup_corrupt_config(file_path, f"JSONDecodeError: {e}")
        return {}
    except OSError as e:
        _backup_corrupt_config(file_path, f"OSError: {e}")
        return {}


def write_json(file_path: str, data: dict) -> None:
    dir_path = os.path.dirname(file_path)
    if dir_path:
        os.makedirs(dir_path, exist_ok=True)
    fd, temp_path = tempfile.mkstemp(prefix="scoop-", dir=dir_path or ".")
    try:
        with os.fdopen(fd, "w", encoding="utf-8") as f:
            json.dump(data, f, indent=2)
        os.replace(temp_path, file_path)
    except Exception:
        try:
            os.unlink(temp_path)
        except OSError:
            pass
        raise


def merge_settings(target: dict, source: dict) -> bool:
    changed = False
    for key, value in source.items():
        if isinstance(value, dict):
            if key not in target or not isinstance(target.get(key), dict):
                target[key] = {}
                changed = True

            if merge_settings(target[key], value):
                changed = True
        else:
            if key not in target or target[key] != value:
                target[key] = value
                changed = True
    return changed


def check_installed(args: dict, request_id: str) -> dict:
    installed = (
        shutil.which("scoop.exe") is not None
        or shutil.which("scoop.ps1") is not None
        or shutil.which("scoop.cmd") is not None
        or shutil.which("scoop") is not None
    )

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
            log(f"Would update {config_path} with new settings")
            return {
                "requestId": request_id,
                "success": True,
                "changed": changed,
                "data": None,
            }

        write_json(config_path, current_config)
        log(f"Updated Scoop config: {config_path}")

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
            "error": str(e),
            "data": None,
        }


def main():
    input_data = sys.stdin.read()
    if not input_data:
        response = {
            "requestId": "unknown",
            "success": False,
            "changed": False,
            "error": "No input provided on stdin",
            "data": None,
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
            "success": False,
            "changed": False,
            "error": f"Failed to parse JSON request: {str(e)}",
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
    except Exception as fatal_err:
        response["error"] = f"Internal Script Error: {str(fatal_err)}"

    sys.stdout.write(json.dumps(response) + "\n")
    sys.stdout.flush()


if __name__ == "__main__":
    main()
