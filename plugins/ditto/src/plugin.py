import json
import os
import sys
import tempfile

CONFIG_PATH = os.path.join(os.environ.get("APPDATA", ""), "Ditto", "Ditto.settings")


def log(msg):
    sys.stderr.write(f"[ditto-plugin] {msg}\n")
    sys.stderr.flush()


def read_json(file_path: str) -> dict:
    if not os.path.exists(file_path):
        return {}
    try:
        with open(file_path, "r", encoding="utf-8") as f:
            return json.load(f)
    except Exception as e:
        log(f"Warning: could not parse {file_path}: {e}")
        return {}


def write_json_atomic(file_path: str, data: dict) -> None:
    dir_name = os.path.dirname(file_path)
    os.makedirs(dir_name, exist_ok=True)
    fd, tmp_path = tempfile.mkstemp(dir=dir_name)
    try:
        with os.fdopen(fd, "w", encoding="utf-8") as f:
            json.dump(data, f, indent=2)
            f.write("\n")
        os.replace(tmp_path, file_path)
    except Exception:
        os.unlink(tmp_path)
        raise


def merge_settings(target: dict, source: dict) -> bool:
    changed = False
    for key, value in source.items():
        if key not in target or target[key] != value:
            target[key] = value
            changed = True
    return changed


def check_installed() -> bool:
    appdata = os.environ.get("APPDATA", "")
    exe_path = os.path.join(appdata, "Ditto", "Ditto.exe")

    installed = os.path.exists(exe_path)

    if not installed:
        for folder in os.environ.get("PATH", "").split(os.pathsep):
            if os.path.exists(os.path.join(folder, "Ditto.exe")):
                installed = True
                break

    return installed


def apply_config(args: dict, context: dict, request_id: str) -> dict:
    new_settings = args.get("settings", {})

    if not isinstance(new_settings, dict):
        return {"requestId": request_id, "error": "settings must be a dictionary"}

    dry_run = args.get("dryRun", False)

    current = read_json(CONFIG_PATH)
    changed = merge_settings(current, new_settings)

    if not changed:
        log("No changes needed.")
        return {"requestId": request_id, "changed": False}

    if dry_run:
        log(f"Would update {CONFIG_PATH} with: {json.dumps(new_settings)}")
        return {"requestId": request_id, "changed": True}

    try:
        write_json_atomic(CONFIG_PATH, current)
        log(f"Updated {CONFIG_PATH}")
        return {"requestId": request_id, "changed": True}
    except Exception as e:
        log(f"Error writing config: {e}")
        return {
            "requestId": request_id,
            "changed": False,
            "error": str(e),
        }


def main():
    input_data = sys.stdin.read()
    if not input_data:
        response = {"requestId": "unknown", "error": "Empty stdin"}
        sys.stdout.write(json.dumps(response) + "\n")
        sys.stdout.flush()
        return

    try:
        request = json.loads(input_data)
    except Exception as e:
        log(f"Failed to parse request: {e}")
        response = {"requestId": "unknown", "error": f"Invalid JSON: {str(e)}"}
        sys.stdout.write(json.dumps(response) + "\n")
        sys.stdout.flush()
        return

    request_id = request.get("requestId") or "unknown"
    command = request.get("command")
    args = request.get("args", {})
    context = request.get("context", {})

    try:
        if command == "check_installed":
            installed = check_installed()
            response = {"requestId": request_id, "installed": installed}
            sys.stdout.write(json.dumps(response) + "\n")
            sys.stdout.flush()
            return

        elif command == "apply":
            response = apply_config(args, context, request_id)
        else:
            response = {"requestId": request_id, "error": f"Unknown command: {command}"}

    except Exception as fatal_err:
        response = {"requestId": request_id, "error": f"Internal Script Error: {str(fatal_err)}"}

    sys.stdout.write(json.dumps(response) + "\n")
    sys.stdout.flush()


if __name__ == "__main__":
    main()
