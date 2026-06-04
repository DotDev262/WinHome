import json
import os
import shutil
import sys
import tempfile
import uuid


def log(msg):
    sys.stderr.write(f"[alacritty-plugin] {msg}\n")
    sys.stderr.flush()


def get_config_path() -> str:
    if sys.platform == "win32":
        appdata = os.environ.get("APPDATA")
        if appdata:
            return os.path.join(appdata, "alacritty", "alacritty.toml")
    return os.path.expanduser("~/.config/alacritty/alacritty.toml")


def read_toml(file_path: str) -> dict:
    if not os.path.exists(file_path):
        return {}

    try:
        if sys.version_info >= (3, 11):
            import tomllib

            with open(file_path, "rb") as f:
                data = tomllib.load(f)
                return data if isinstance(data, dict) else {}
        else:
            import tomli

            with open(file_path, "rb") as f:
                data = tomli.load(f)
                return data if isinstance(data, dict) else {}
    except ModuleNotFoundError:
        raise
    except Exception as e:
        backup_path = f"{file_path}.{uuid.uuid4().hex}.bak"
        log(f"Failed to parse alacritty.toml: {e}. Backing up to {backup_path}")
        try:
            shutil.copy2(file_path, backup_path)
        except Exception as backup_err:
            log(f"Failed to create backup: {backup_err}")
        return {}


def toml_value(value) -> str:
    if isinstance(value, bool):
        return "true" if value else "false"
    if isinstance(value, (int, float)):
        return str(value)
    if isinstance(value, str):
        # Escape quotes and backslashes for TOML strings
        escaped = value.replace("\\", "\\\\").replace('"', '\\"')
        return f'"{escaped}"'
    if isinstance(value, list):
        return "[" + ", ".join(toml_value(v) for v in value) + "]"
    raise ValueError(f"Unsupported TOML value type: {type(value).__name__}")


def toml_lines(data: dict, prefix: str = "") -> list:
    lines = []
    # First emit primitives
    for k, v in data.items():
        if not isinstance(v, dict):
            lines.append(f"{k} = {toml_value(v)}")

    # Then emit nested dicts as tables
    for k, v in data.items():
        if isinstance(v, dict):
            table_name = f"{prefix}.{k}" if prefix else k
            lines.append("")
            lines.append(f"[{table_name}]")
            lines.extend(toml_lines(v, table_name))
    return lines


def write_toml(file_path: str, data: dict) -> None:
    dir_path = os.path.dirname(file_path) or "."
    os.makedirs(dir_path, exist_ok=True)

    fd, temp_path = tempfile.mkstemp(dir=dir_path, prefix="alacritty.toml.")
    try:
        lines = toml_lines(data)
        with os.fdopen(fd, "w", encoding="utf-8") as f:
            f.write("\n".join(lines).strip() + "\n")
        os.replace(temp_path, file_path)
    except Exception:
        os.unlink(temp_path)
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
    installed = shutil.which("alacritty.exe") is not None or shutil.which("alacritty") is not None
    return {
        "requestId": request_id,
        "success": True,
        "changed": False,
        "data": installed,
    }


def apply_config(args: dict, context: dict, request_id: str) -> dict:
    dry_run = bool(context.get("dryRun", False))
    settings = args.get("settings", {}) or {}

    try:
        config_path = get_config_path()
        current_config = read_toml(config_path)
        changed = merge_settings(current_config, settings)

        if not changed:
            return {
                "requestId": request_id,
                "success": True,
                "changed": False,
                "data": None,
            }

        if dry_run:
            log(f"dry_run: would update {config_path}")
            return {
                "requestId": request_id,
                "success": True,
                "changed": changed,
                "data": None,
            }

        write_toml(config_path, current_config)
        log(f"Updated config: {config_path}")

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


def handle(request: dict) -> dict:
    request_id = request.get("requestId", "unknown")
    command = request.get("command")
    args = request.get("args", {})
    context = request.get("context", {})

    if command == "check_installed":
        return check_installed(args, request_id)

    if command == "apply":
        if not isinstance(args, dict):
            return {
                "requestId": request_id,
                "success": False,
                "changed": False,
                "error": "args must be an object",
                "data": None,
            }
        if not isinstance(context, dict):
            return {
                "requestId": request_id,
                "success": False,
                "changed": False,
                "error": "context must be an object",
                "data": None,
            }
        settings = args.get("settings")
        if settings is not None and not isinstance(settings, dict):
            return {
                "requestId": request_id,
                "success": False,
                "changed": False,
                "error": "settings must be an object",
                "data": None,
            }
        return apply_config(args, context, request_id)

    return {
        "requestId": request_id,
        "success": False,
        "changed": False,
        "error": f"Unknown command: {command}",
        "data": None,
    }


def main() -> None:
    raw = sys.stdin.read()
    if not raw or not raw.strip():
        sys.stdout.write(
            json.dumps(
                {
                    "requestId": "unknown",
                    "success": False,
                    "changed": False,
                    "error": "Empty input",
                    "data": None,
                }
            )
            + "\n"
        )
        sys.stdout.flush()
        return

    try:
        request = json.loads(raw)
        result = handle(request)
    except Exception as error:
        request_id = "unknown"
        if "request" in locals() and isinstance(request, dict):
            request_id = request.get("requestId", "unknown")
        result = {
            "requestId": request_id,
            "success": False,
            "changed": False,
            "error": str(error),
            "data": None,
        }

    sys.stdout.write(json.dumps(result) + "\n")
    sys.stdout.flush()


if __name__ == "__main__":
    main()
