# /// script
# dependencies = []
# ///

import json
import os
import sys
import tempfile


def log(msg):
    sys.stderr.write(f"[sdkman-plugin] {msg}\n")
    sys.stderr.flush()


def get_sdkman_dir():
    userprofile = os.environ.get("USERPROFILE")

    if userprofile:
        return os.path.join(userprofile, ".sdkman")

    return os.path.expanduser("~/.sdkman")


def get_config_path():
    return os.path.join(
        get_sdkman_dir(),
        "etc",
        "config",
    )


def read_config(file_path: str) -> dict:
    config = {}

    if not os.path.exists(file_path):
        return config

    with open(file_path, "r", encoding="utf-8") as f:
        for line in f:
            line = line.strip()

            if not line or "=" not in line:
                continue

            key, value = line.split("=", 1)

            config[key.strip()] = value.strip()

    return config


def sdkman_value(value):
    if value is None:
        return None

    if isinstance(value, bool):
        return "true" if value else "false"

    return str(value)


def write_config(file_path: str, data: dict):
    os.makedirs(os.path.dirname(file_path), exist_ok=True)

    fd, temp_path = tempfile.mkstemp(dir=os.path.dirname(file_path))

    try:
        with os.fdopen(fd, "w", encoding="utf-8") as f:
            for key, value in data.items():
                f.write(f"{key}={sdkman_value(value)}\n")

        os.replace(temp_path, file_path)

    finally:
        if os.path.exists(temp_path):
            try:
                os.remove(temp_path)
            except OSError:
                pass


def merge_settings(target: dict, source: dict) -> bool:
    changed = False

    for key, value in source.items():
        converted = sdkman_value(value)

        if converted is None:
            continue

        if key not in target or target[key] != converted:
            target[key] = converted
            changed = True

    return changed


def check_installed(args: dict, request_id: str) -> dict:
    installed = os.path.isdir(get_sdkman_dir())

    return {
        "requestId": request_id,
        "success": True,
        "changed": False,
        "data": installed,
    }


def apply_config(args: dict, context: dict, request_id: str) -> dict:
    dry_run = bool(context.get("dryRun", False))
    settings = args.get("settings", {})

    if not isinstance(settings, dict):
        raise ValueError("settings must be an object")

    try:
        config_path = get_config_path()
        current_config = read_config(config_path)
        changed = merge_settings(current_config, settings)

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
                "changed": changed,
            }

        write_config(config_path, current_config)
        log(f"Updated SDKMAN config: {config_path}")

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
        if not isinstance(args, dict):
            raise ValueError("args must be an object")
        return check_installed(args, request_id)
    if command == "apply":
        if not isinstance(args, dict):
            raise ValueError("args must be an object")
        if not isinstance(context, dict):
            raise ValueError("context must be an object")
        return apply_config(args, context, request_id)

    return {
        "requestId": request_id,
        "success": False,
        "changed": False,
        "error": f"Unknown command: {command}",
    }


def main() -> None:
    raw = sys.stdin.read()
    if not raw:
        return

    try:
        request = json.loads(raw)
        result = handle(request)
    except Exception as error:
        result = {
            "requestId": request.get("requestId", "unknown")
            if "request" in locals() and isinstance(request, dict)
            else "unknown",
            "success": False,
            "changed": False,
            "error": str(error),
        }

    sys.stdout.write(json.dumps(result) + "\n")
    sys.stdout.flush()


if __name__ == "__main__":
    main()
