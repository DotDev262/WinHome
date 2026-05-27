import json
import os
import shutil
import sys
import tempfile
import uuid

try:
    import tomllib
except ImportError:
    try:
        import tomli as tomllib
    except ImportError:
        tomllib = None


def log(msg: str) -> None:
    sys.stderr.write(f"[topgrade-plugin] {msg}\n")
    sys.stderr.flush()


def get_config_path() -> str:
    appdata = os.environ.get("APPDATA")
    if not appdata:
        raise Exception("APPDATA environment variable not found")

    primary = os.path.join(appdata, "topgrade.toml")
    fallback = os.path.join(appdata, "topgrade", "topgrade.toml")

    if os.path.exists(primary):
        return primary
    if os.path.exists(fallback):
        return fallback
    return primary


def read_toml(file_path: str) -> dict:
    if not os.path.exists(file_path):
        return {}

    if tomllib is None:
        raise Exception("No TOML library available (Python 3.11+ tomllib or tomli required)")

    try:
        with open(file_path, "rb") as f:
            data = tomllib.load(f)
            return data if isinstance(data, dict) else {}
    except Exception as e:
        backup_path = f"{file_path}.{uuid.uuid4().hex}.bak"
        log(f"Failed to parse {file_path}: {e}. Backing up to {backup_path}")
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
        escaped = value.replace("\\", "\\\\").replace('"', '\\"')
        return f'"{escaped}"'
    if isinstance(value, list):
        return "[" + ", ".join(toml_value(v) for v in value) + "]"
    if isinstance(value, dict):
        if not value:
            return "{}"
        return "{ " + ", ".join(f"{toml_key(k)} = {toml_value(v)}" for k, v in value.items()) + " }"
    raise ValueError(f"Unsupported TOML value type: {type(value).__name__}")


def toml_key(key) -> str:
    if any(c in key for c in ' .~/\'"@:[] \t'):
        escaped = key.replace("\\", "\\\\").replace('"', '\\"')
        return f'"{escaped}"'
    return key


def toml_lines(data: dict, prefix: str = "") -> list:
    lines = []
    # Primitives, lists, and empty dicts first
    for k, v in data.items():
        if not isinstance(v, dict) or (isinstance(v, dict) and not v):
            lines.append(f"{toml_key(k)} = {toml_value(v)}")

    # Nested non-empty dicts as tables next
    for k, v in data.items():
        if isinstance(v, dict) and v:
            table_name = f"{prefix}.{toml_key(k)}" if prefix else toml_key(k)
            lines.append("")
            lines.append(f"[{table_name}]")
            lines.extend(toml_lines(v, table_name))
    return lines


def write_toml(file_path: str, data: dict) -> None:
    dir_path = os.path.dirname(file_path) or "."
    os.makedirs(dir_path, exist_ok=True)

    fd, temp_path = tempfile.mkstemp(dir=dir_path, prefix="topgrade.toml.")
    try:
        lines = toml_lines(data)
        with os.fdopen(fd, "w", encoding="utf-8") as f:
            f.write("\n".join(lines).strip() + "\n")
        os.replace(temp_path, file_path)
    except Exception:
        if os.path.exists(temp_path):
            os.unlink(temp_path)
        raise


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


def check_installed_cmd(args: dict, request_id: str) -> dict:
    installed = (
        shutil.which("topgrade.exe") is not None
        or shutil.which("topgrade") is not None
    )
    return {
        "requestId": request_id,
        "success": True,
        "changed": False,
        "data": {"installed": installed},
    }


def apply_config(args: dict, context: dict, request_id: str) -> dict:
    dry_run = bool(context.get("dryRun", False))
    settings = args.get("settings", {}) or {}

    if not isinstance(settings, dict):
        return {
            "requestId": request_id,
            "success": False,
            "changed": False,
            "error": "settings must be an object",
        }

    try:
        config_path = get_config_path()
        current_config = read_toml(config_path)
        changed = deep_merge(current_config, settings)

        if not changed:
            return {
                "requestId": request_id,
                "success": True,
                "changed": False,
            }

        if dry_run:
            log(f"dry_run: would update {config_path}")
            return {
                "requestId": request_id,
                "success": True,
                "changed": True,
            }

        write_toml(config_path, current_config)
        log(f"Updated config: {config_path}")

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


def handle(request: dict) -> dict:
    request_id = request.get("requestId", "unknown")
    command = request.get("command")
    args = request.get("args", {})
    context = request.get("context", {})

    if command == "check_installed":
        return check_installed_cmd(args, request_id)

    if command == "apply":
        if not isinstance(args, dict):
            return {
                "requestId": request_id,
                "success": False,
                "changed": False,
                "error": "args must be an object",
            }
        if not isinstance(context, dict):
            return {
                "requestId": request_id,
                "success": False,
                "changed": False,
                "error": "context must be an object",
            }
        return apply_config(args, context, request_id)

    return {
        "requestId": request_id,
        "success": False,
        "changed": False,
        "error": f"Unknown command: {command}",
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
        }

    sys.stdout.write(json.dumps(result) + "\n")
    sys.stdout.flush()


if __name__ == "__main__":
    main()
