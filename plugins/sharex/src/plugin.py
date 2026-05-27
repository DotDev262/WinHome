import datetime
import json
import os
import shutil
import sys
import uuid


def log(msg):
    sys.stderr.write(f"[sharex-plugin] {msg}\n")
    sys.stderr.flush()


def get_config_path():
    appdata = os.getenv("APPDATA")
    if not appdata:
        raise Exception("APPDATA environment variable not found")
    return os.path.join(appdata, "ShareX", "ShareX.json")


def read_json(file_path: str) -> dict:
    if not os.path.exists(file_path):
        return {}
    try:
        with open(file_path, "r", encoding="utf-8") as f:
            return json.load(f)
    except json.JSONDecodeError:
        timestamp = datetime.datetime.now(datetime.timezone.utc).strftime(
            "%Y%m%d%H%M%S"
        )
        suffix = uuid.uuid4().hex[:8]
        backup_path = f"{file_path}.corrupted.{timestamp}.{suffix}"
        try:
            shutil.move(file_path, backup_path)
            log(
                f"Config corrupted. Backing up to {backup_path} and starting fresh."
            )
        except Exception as backup_e:
            log(f"Failed to backup corrupted config: {backup_e}")
        return {}
    except OSError as e:
        raise OSError(f"Could not read {file_path}: {e}") from e


def write_json(file_path: str, data: dict) -> None:
    os.makedirs(os.path.dirname(file_path), mode=0o700, exist_ok=True)
    tmp_path = file_path + ".tmp"
    with open(tmp_path, "w", encoding="utf-8") as f:
        json.dump(data, f, indent=2)
        f.write("\n")
    os.replace(tmp_path, file_path)


def deep_merge(target: dict, source: dict) -> bool:
    changed = False
    for k, v in source.items():
        if k in target and isinstance(target[k], dict) and isinstance(v, dict):
            if deep_merge(target[k], v):
                changed = True
        else:
            if k not in target or target[k] != v:
                target[k] = v
                changed = True
    return changed


def check_installed(args: dict, request_id: str) -> dict:
    installed = False
    if shutil.which("ShareX.exe") or shutil.which("ShareX"):
        installed = True
    else:
        program_files = os.getenv("PROGRAMFILES", "C:\\Program Files")
        if os.path.exists(os.path.join(program_files, "ShareX", "ShareX.exe")):
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

        changed = deep_merge(current_config, settings)

        if not changed:
            return {
                "requestId": request_id,
                "success": True,
                "changed": False,
            }

        if dry_run:
            log(f"Would update {config_path} with new settings")
            return {
                "requestId": request_id,
                "success": True,
                "changed": True,
            }

        write_json(config_path, current_config)
        log(f"Updated ShareX config: {config_path}")

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
        response = {
            "requestId": "unknown",
            "success": False,
            "changed": False,
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
