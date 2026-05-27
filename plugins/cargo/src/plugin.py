# /// script
# dependencies = []
# ///

import json
import os
import shutil
import sys


def log(msg):
    sys.stderr.write(f"[cargo-plugin] {msg}\n")
    sys.stderr.flush()


def get_config_path() -> str:
    userprofile = os.environ.get("USERPROFILE", "")
    return os.path.join(userprofile, ".cargo", "config.toml")


def read_toml(file_path: str) -> dict:
    if not os.path.exists(file_path):
        return {}
    try:
        import tomllib
    except ImportError:
        try:
            import tomli as tomllib
        except ImportError:
            raise RuntimeError("tomllib (Python 3.11+) or tomli is required")
    with open(file_path, "rb") as f:
        return tomllib.load(f)


def write_toml(file_path: str, data: dict) -> None:
    os.makedirs(os.path.dirname(file_path), exist_ok=True)
    lines = []
    for key, value in data.items():
        if not isinstance(value, dict):
            lines.append(f"{key} = {toml_value(value)}")
    for section, contents in data.items():
        if isinstance(contents, dict):
            lines.append(f"\n[{section}]")
            for k, v in contents.items():
                lines.append(f"{k} = {toml_value(v)}")
    temp_path = file_path + ".tmp"
    with open(temp_path, "w", encoding="utf-8") as f:
        f.write("\n".join(lines).strip() + "\n")
    os.replace(temp_path, file_path)


def toml_value(value) -> str:
    if isinstance(value, bool):
        return "true" if value else "false"
    elif isinstance(value, int):
        return str(value)
    elif isinstance(value, float):
        return str(value)
    elif isinstance(value, str):
        return f'"{value}"'
    elif isinstance(value, list):
        items = ", ".join(toml_value(i) for i in value)
        return f"[{items}]"
    else:
        return f'"{str(value)}"'


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
    cargo_home = os.environ.get("CARGO_HOME", "")
    installed = (
        shutil.which("cargo.exe") is not None
        or shutil.which("cargo") is not None
        or (
            cargo_home != ""
            and os.path.exists(os.path.join(cargo_home, "bin", "cargo.exe"))
        )
    )
    return {
        "requestId": request_id,
        "success": True,
        "changed": False,
        "data": installed,
    }


def apply_config(args: dict, context: dict, request_id: str) -> dict:
    dry_run = bool(context.get("dryRun", False))
    settings = args.get("settings", {})

    try:
        config_path = get_config_path()
        current_config = read_toml(config_path)
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

        write_toml(config_path, current_config)
        log(f"Updated Cargo config: {config_path}")

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
