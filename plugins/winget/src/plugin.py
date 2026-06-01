import json
import os
import shutil
import sys

PACKAGE_FAMILY = "Microsoft.DesktopAppInstaller_8wekyb3d8bbwe"


def log(msg: str) -> None:
    sys.stderr.write(f"[winget-plugin] {msg}\n")
    sys.stderr.flush()


def get_settings_path() -> str:
    local_app_data = os.getenv("LOCALAPPDATA")
    if not local_app_data:
        raise Exception("LOCALAPPDATA environment variable not found")

    return os.path.join(
        local_app_data,
        "Packages",
        PACKAGE_FAMILY,
        "LocalState",
        "settings.json",
    )


def read_json(file_path: str) -> dict:
    if not os.path.exists(file_path):
        return {}

    try:
        with open(file_path, "r", encoding="utf-8-sig") as f:
            data = json.load(f)
            return data if isinstance(data, dict) else {}
    except json.JSONDecodeError as e:
        raise Exception(f"Could not parse {file_path}: {e}") from e


def write_json(file_path: str, data: dict) -> None:
    os.makedirs(os.path.dirname(file_path), exist_ok=True)
    tmp_path = file_path + ".tmp"
    with open(tmp_path, "w", encoding="utf-8") as f:
        json.dump(data, f, indent=4)
        f.write("\n")
    os.replace(tmp_path, file_path)


def deep_merge(target: dict, source: dict) -> bool:
    changed = False

    for key, value in source.items():
        if isinstance(value, dict):
            if key not in target or not isinstance(target.get(key), dict):
                target[key] = {}
                changed = True

            if deep_merge(target[key], value):
                changed = True
        else:
            if key not in target or target[key] != value:
                target[key] = value
                changed = True

    return changed


def check_installed(args: dict, request_id: str) -> dict:
    installed = shutil.which("winget.exe") is not None or shutil.which("winget") is not None
    return {
        "requestId": request_id,
        "success": True,
        "changed": False,
        "data": {"installed": installed},
    }


def apply_config(args: dict, context: dict, request_id: str) -> dict:
    dry_run = context.get("dryRun", False)
    settings = args.get("settings", args)

    if not isinstance(settings, dict):
        return {
            "requestId": request_id,
            "success": False,
            "changed": False,
            "error": "settings must be an object",
        }

    try:
        settings_path = get_settings_path()
        current_settings = read_json(settings_path)
        changed = deep_merge(current_settings, settings)

        if not changed:
            return {
                "requestId": request_id,
                "success": True,
                "changed": False,
            }

        if dry_run:
            log(f"Would update winget settings: {settings_path}")
            return {
                "requestId": request_id,
                "success": True,
                "changed": True,
            }

        write_json(settings_path, current_settings)
        log(f"Updated winget settings: {settings_path}")

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


def main() -> None:
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
