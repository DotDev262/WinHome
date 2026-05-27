import sys
import json
import os
import shutil
import datetime
import uuid
import tempfile

def log(msg):
    sys.stderr.write(f"[curl-plugin] {msg}\n")
    sys.stderr.flush()

def get_config_path():
    if os.name == 'nt':
        user_profile = os.getenv("USERPROFILE")
        if not user_profile:
            user_profile = os.path.expanduser("~")
        return os.path.join(user_profile, "_curlrc")
    else:
        return os.path.expanduser("~/.curlrc")

def _backup_corrupt_config(file_path: str, reason: str):
    timestamp = datetime.datetime.now(datetime.timezone.utc).strftime("%Y%m%d%H%M%S")
    suffix = uuid.uuid4().hex[:8]
    backup_path = f"{file_path}.corrupted.{timestamp}.{suffix}"
    log(f"Config read failed ({reason}). Backing up to {backup_path} and starting fresh.")
    try:
        shutil.move(file_path, backup_path)
    except Exception as backup_e:
        log(f"Failed to backup corrupted config: {backup_e}")

def parse_curlrc(lines: list) -> dict:
    config = {}
    for line in lines:
        line = line.strip()
        if not line or line.startswith("#"):
            continue
        if "=" in line:
            key, val = line.split("=", 1)
            config[key.strip()] = val.strip()
        else:
            config[line] = None
    return config

def read_curlrc(file_path: str) -> dict:
    if not os.path.exists(file_path):
        return {}
    
    try:
        with open(file_path, "r", encoding="utf-8") as f:
            lines = f.readlines()
            return parse_curlrc(lines)
    except UnicodeDecodeError as e:
        _backup_corrupt_config(file_path, f"UnicodeDecodeError: {e}")
        return {}
    except OSError as e:
        _backup_corrupt_config(file_path, f"OSError: {e}")
        return {}

def build_curlrc_content(merged_config: dict) -> str:
    lines = []
    for key, value in merged_config.items():
        if value is None or str(value).lower() in ("true", "null"):
            # It's a flag
            lines.append(key)
        elif str(value).lower() == "false":
            # If the user sets a flag to false, we remove it.
            continue
        else:
            lines.append(f"{key}={value}")
    return "\n".join(lines) + "\n"

def write_curlrc(file_path: str, merged_config: dict) -> None:
    dir_path = os.path.dirname(file_path)
    if dir_path:
        os.makedirs(dir_path, exist_ok=True)
    fd, temp_path = tempfile.mkstemp(prefix="curl-", dir=dir_path or ".")
    try:
        with os.fdopen(fd, "w", encoding="utf-8") as f:
            f.write(build_curlrc_content(merged_config))
        os.replace(temp_path, file_path)
    except Exception:
        try:
            os.unlink(temp_path)
        except OSError:
            pass
        raise

def _to_str(v):
    if v is None:
        return None
    return str(v).lower() if isinstance(v, bool) else str(v)

def merge_settings(target: dict, source: dict) -> bool:
    changed = False
    for key, value in source.items():
        nv = _to_str(value)
        if key not in target:
            target[key] = value
            changed = True
        elif target[key] != nv:
            if not (target[key] is None and nv == "true"):
                target[key] = value
                changed = True
    return changed

def check_installed(args: dict, request_id: str) -> dict:
    installed = shutil.which("curl.exe") is not None or \
                shutil.which("curl") is not None
                
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
        current_config = read_curlrc(config_path)
        
        changed = merge_settings(current_config, settings)

        if not changed:
            return {
                "requestId": request_id,
                "success": True,
                "changed": False,
                "data": None
            }

        if dry_run:
            log(f"Would update {config_path} with new settings")
            return {
                "requestId": request_id,
                "success": True,
                "changed": changed,
                "data": None
            }

        write_curlrc(config_path, current_config)
        log(f"Updated curl config: {config_path}")

        return {
            "requestId": request_id,
            "success": True,
            "changed": True,
            "data": None
        }

    except Exception as e:
        log(f"Failed to apply config: {e}")
        return {
            "requestId": request_id,
            "success": False,
            "changed": False,
            "error": str(e),
            "data": None
        }

def main():
    input_data = sys.stdin.read()
    if not input_data:
        response = {
            "requestId": "unknown",
            "success": False,
            "changed": False,
            "error": "No input provided on stdin",
            "data": None
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
            "data": None
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
        "data": None
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
