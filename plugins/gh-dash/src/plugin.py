# /// script
# dependencies = [
#   "pyyaml",
# ]
# ///

import json
import os
import shutil
import subprocess
import sys

try:
    import yaml
except ImportError:
    yaml = None


def get_config_path() -> str:
    env_path = os.environ.get("GH_DASH_CONFIG")
    if env_path:
        return env_path
    user_profile = os.environ.get("USERPROFILE", "")
    return os.path.join(user_profile, ".config", "gh-dash", "config.yml")


def log(message: str) -> None:
    sys.stderr.write(f"[gh-dash-plugin] {message}\n")
    sys.stderr.flush()


def read_yaml(file_path: str) -> dict:
    if yaml is None:
        raise RuntimeError("PyYAML is required to read or write gh-dash config")

    if not os.path.exists(file_path):
        return {}

    with open(file_path, "r", encoding="utf-8") as fh:
        data = yaml.safe_load(fh)
        return data if isinstance(data, dict) else {}


def write_yaml(file_path: str, data: dict) -> None:
    if yaml is None:
        raise RuntimeError("PyYAML is required to read or write gh-dash config")

    os.makedirs(os.path.dirname(file_path), exist_ok=True)
    with open(file_path, "w", encoding="utf-8") as fh:
        yaml.dump(data, fh, default_flow_style=False, sort_keys=False)


def merge_settings(target: dict, source: dict) -> bool:
    """Merge source into target. Lists are replaced entirely; dicts are merged recursively."""
    changed = False
    for key, value in source.items():
        if isinstance(value, list):
            if target.get(key) != value:
                target[key] = value
                changed = True
        elif isinstance(value, dict):
            if not isinstance(target.get(key), dict):
                target[key] = {}
                changed = True
            if merge_settings(target[key], value):
                changed = True
        else:
            if target.get(key) != value:
                target[key] = value
                changed = True
    return changed


def get_settings_from_args(args: dict) -> dict:
    """Extract settings from args, supporting both wrapped and flat formats."""
    settings = args.get("settings")
    if isinstance(settings, dict):
        return settings
    return args


def check_installed(request_id: str) -> dict:
    if shutil.which("gh-dash") is not None or shutil.which("gh-dash.exe") is not None:
        return {"requestId": request_id, "success": True, "changed": False, "data": {"installed": True}}

    try:
        result = subprocess.run(
            ["gh", "ext", "list"],
            capture_output=True,
            text=True,
            timeout=5,
        )
        if "dlvhdr/gh-dash" in result.stdout:
            return {"requestId": request_id, "success": True, "changed": False, "data": {"installed": True}}
    except Exception:
        pass

    return {"requestId": request_id, "success": True, "changed": False, "data": {"installed": False}}


def apply_config(request_id: str, args: dict, context: dict) -> dict:
    dry_run = bool(context.get("dryRun", False))
    settings = get_settings_from_args(args)

    config_path = get_config_path()
    current_config = read_yaml(config_path)
    changed = merge_settings(current_config, settings)

    if dry_run:
        if changed:
            log(f"dry_run: would update {config_path}")
        else:
            log(f"dry_run: no changes for {config_path}")
        return {"requestId": request_id, "success": True, "changed": changed}

    if changed:
        write_yaml(config_path, current_config)

    return {"requestId": request_id, "success": True, "changed": changed}


def handle(request: dict) -> dict:
    request_id = request.get("requestId", "unknown")
    command = request.get("command")
    args = request.get("args", {})
    context = request.get("context", {})

    if command == "check_installed":
        return check_installed(request_id)
    if command == "apply":
        if not isinstance(args, dict):
            raise ValueError("args must be an object")
        if not isinstance(context, dict):
            raise ValueError("context must be an object")
        return apply_config(request_id, args, context)

    raise ValueError(f"Unknown command: {command}")


def main() -> None:
    raw = sys.stdin.read()
    if not raw:
        return

    try:
        request = json.loads(raw)
        result = handle(request)
    except Exception as error:
        result = {
            "requestId": request.get("requestId", "unknown") if "request" in locals() and isinstance(request, dict) else "unknown",
            "success": False,
            "changed": False,
            "error": str(error),
        }

    sys.stdout.write(json.dumps(result) + "\n")
    sys.stdout.flush()


if __name__ == "__main__":
    main()
