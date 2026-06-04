import datetime
import json
import os
import shutil
import sys
import tempfile
import uuid


def log(msg):
    sys.stderr.write(f"[ripgrep-plugin] {msg}\n")
    sys.stderr.flush()


def get_config_path():
    env_path = os.getenv("RIPGREP_CONFIG_PATH")
    if env_path:
        return env_path

    if os.name == "nt":
        user_profile = os.getenv("USERPROFILE") or os.path.expanduser("~")
        return os.path.join(user_profile, ".ripgreprc")

    return os.path.expanduser("~/.ripgreprc")


def _backup_corrupt_config(file_path: str, reason: str):
    timestamp = datetime.datetime.now(datetime.timezone.utc).strftime("%Y%m%d%H%M%S")
    suffix = uuid.uuid4().hex[:8]
    backup_path = f"{file_path}.corrupted.{timestamp}.{suffix}"
    log(f"Config read failed ({reason}). Backing up to {backup_path} and starting fresh.")

    try:
        shutil.move(file_path, backup_path)
    except Exception as backup_e:
        log(f"Failed to backup corrupted config: {backup_e}")


def parse_ripgreprc(lines: list) -> dict:
    config = {}

    for line in lines:
        line = line.strip()

        if not line or line.startswith("#"):
            continue

        if not line.startswith("--"):
            raise ValueError(f"Invalid ripgrep config line: {line}")

        flag = line[2:]

        if "=" in flag:
            key, value = flag.split("=", 1)
            config[key.strip()] = value.strip()
        else:
            config[flag.strip()] = True

    return config


def read_ripgreprc(file_path: str) -> dict:
    if not os.path.exists(file_path):
        return {}

    try:
        with open(file_path, "r", encoding="utf-8") as f:
            return parse_ripgreprc(f.readlines())
    except (UnicodeDecodeError, ValueError, OSError) as e:
        _backup_corrupt_config(file_path, f"{type(e).__name__}: {e}")
        return {}


def _format_value(value):
    if isinstance(value, bool):
        return None if value else False

    if value is None:
        return None

    return str(value)


def build_ripgreprc_content(config: dict) -> str:
    lines = []

    for key, value in config.items():
        formatted_value = _format_value(value)

        if formatted_value is False:
            continue

        if formatted_value is None:
            lines.append(f"--{key}")
        else:
            lines.append(f"--{key}={formatted_value}")

    return "\n".join(lines) + "\n"


def write_ripgreprc(file_path: str, config: dict) -> None:
    dir_path = os.path.dirname(file_path)

    if dir_path:
        os.makedirs(dir_path, exist_ok=True)

    fd, temp_path = tempfile.mkstemp(prefix="ripgrep-", dir=dir_path or ".")

    try:
        with os.fdopen(fd, "w", encoding="utf-8") as f:
            f.write(build_ripgreprc_content(config))

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
        normalized_value = _format_value(value)

        if normalized_value is False:
            if key in target:
                del target[key]
                changed = True
            continue

        if key not in target or _format_value(target[key]) != normalized_value:
            target[key] = value
            changed = True

    return changed


def check_installed(args: dict, request_id: str) -> dict:
    installed = shutil.which("rg.exe") is not None or shutil.which("rg") is not None

    return {
        "requestId": request_id,
        "success": True,
        "changed": False,
        "data": installed,
    }


def apply_config(args: dict, context: dict, request_id: str) -> dict:
    dry_run = context.get("dryRun", False)
    settings = args.get("settings", {})

    if not isinstance(settings, dict):
        return {
            "requestId": request_id,
            "success": False,
            "changed": False,
            "error": "settings must be an object",
            "data": None,
        }

    try:
        config_path = get_config_path()
        current_config = read_ripgreprc(config_path)
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
                "changed": True,
                "data": {
                    "path": config_path,
                    "settings": settings,
                },
            }

        write_ripgreprc(config_path, current_config)
        log(f"Updated ripgrep config: {config_path}")

        return {
            "requestId": request_id,
            "success": True,
            "changed": True,
            "data": {
                "path": config_path,
            },
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
            "data": None,
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
