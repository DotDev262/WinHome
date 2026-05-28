import json
import os
import shutil
import sys
import tempfile
import uuid


def log(msg):
    sys.stderr.write(f"[chezmoi-plugin] {msg}\n")
    sys.stderr.flush()


def get_config_path() -> str:
    userprofile = os.environ.get("USERPROFILE")
    if userprofile and sys.platform == "win32":
        return os.path.join(userprofile, ".config", "chezmoi", "chezmoi.yaml")
    return os.path.expanduser("~/.config/chezmoi/chezmoi.yaml")


def read_yaml(file_path: str) -> dict:
    if not os.path.exists(file_path):
        return {}

    try:
        import yaml

        with open(file_path, "r", encoding="utf-8") as f:
            data = yaml.safe_load(f)
            return data if isinstance(data, dict) else {}
    except Exception as e:
        backup_path = f"{file_path}.{uuid.uuid4().hex}.bak"
        log(f"Failed to parse chezmoi.yaml: {e}. Backing up to {backup_path}")
        try:
            shutil.copy2(file_path, backup_path)
        except Exception as backup_err:
            log(f"Failed to create backup: {backup_err}")
        return {}


def write_yaml(file_path: str, data: dict) -> None:
    os.makedirs(os.path.dirname(file_path), exist_ok=True)

    fd, temp_path = tempfile.mkstemp(
        dir=os.path.dirname(file_path), prefix="chezmoi.yaml."
    )
    try:
        import yaml

        with os.fdopen(fd, "w", encoding="utf-8") as f:
            yaml.safe_dump(data, f, default_flow_style=False, sort_keys=False)
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
    installed = (
        shutil.which("chezmoi.exe") is not None
        or shutil.which("chezmoi") is not None
    )
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
        current_config = read_yaml(config_path)
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

        write_yaml(config_path, current_config)
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
