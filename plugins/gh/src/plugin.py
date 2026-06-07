# /// script
# dependencies = [
#   "pyyaml",
# ]
# ///

import json
import os
import shutil
import sys

try:
    import yaml
except ImportError:
    yaml = None


def get_config_path() -> str:
    appdata = os.environ.get("APPDATA")

    if appdata:
        return os.path.join(appdata, "GitHub CLI", "config.yml")

    return os.path.join(
        os.path.expanduser("~"),
        ".config",
        "gh",
        "config.yml",
    )


def log(message: str) -> None:
    sys.stderr.write(f"[gh-plugin] {message}\n")
    sys.stderr.flush()


def read_yaml(file_path: str) -> dict:
    if yaml is None:
        raise RuntimeError("PyYAML is required to read or write gh config")

    if not os.path.exists(file_path):
        return {}

    with open(file_path, "r", encoding="utf-8") as file_handle:
        data = yaml.safe_load(file_handle) or {}
        if not isinstance(data, dict):
            return {}
        return data


def write_yaml(file_path: str, data: dict) -> None:
    if yaml is None:
        raise RuntimeError("PyYAML is required to read or write gh config")

    os.makedirs(os.path.dirname(file_path), exist_ok=True)
    with open(file_path, "w", encoding="utf-8") as file_handle:
        yaml.dump(data, file_handle, default_flow_style=False, sort_keys=False)


def merge_settings(target: dict, source: dict) -> bool:
    changed = False

    for key, value in source.items():
        if value is None:
            continue

        current_value = target.get(key)
        if isinstance(value, dict):
            if not isinstance(current_value, dict):
                target[key] = {}
                current_value = target[key]
                changed = True

            if merge_settings(current_value, value):
                changed = True
        elif current_value != value:
            target[key] = value
            changed = True

    return changed


def get_config_target(config: dict) -> dict:
    nested_args = config.get("args")
    if isinstance(nested_args, dict):
        return nested_args
    return config


def check_installed() -> bool: 
    return ( shutil.which("gh") is not None or shutil.which("gh.exe") is not None )


def apply_config(request_id: str, args: dict) -> dict: 
    dry_run = bool(args.get("dryRun", False)) 
    settings = args.get("settings", {}) 
    if not isinstance(settings, dict): 
        return { "requestId": request_id, 
                "error": "settings must be a dictionary", 
        }
    updates = {key: value for key, value in settings.items()}

    config_path = get_config_path()
    if yaml is None:
        raise RuntimeError("PyYAML is required to read or write gh config")

    current_config = read_yaml(config_path)
    target = get_config_target(current_config)
    changed = merge_settings(target, updates)

    if dry_run:
        if changed:
            log(f"dry_run: would update {config_path}")
        else:
            log(f"dry_run: no changes for {config_path}")
        return {
            "requestId": request_id,
            "changed": changed,
        }

    if changed:
        write_yaml(config_path, current_config)

    return {
        "requestId": request_id,
        "changed": changed,
    }


def handle(request: dict) -> dict:
    request_id = request.get("requestId") or "unknown"
    command = request.get("command")
    args = request.get("args", {})
    context = request.get("context", {})

    if command == "check_installed": 
        installed = check_installed() 
        return { "requestId": request_id, "installed": installed, }
    if command == "apply": 
        if not isinstance(args, dict): 
            return { "requestId": request_id, 
                    "error": "args must be a dictionary", 
            } 
        return apply_config(request_id, args)

    return {
    "requestId": request_id,
    "error": f"Unknown command: {command}",
    }


def main() -> None:
    raw = sys.stdin.read() 
    if not raw: 
        sys.stdout.write( 
            json.dumps({ "requestId": "unknown", "error": "No input received", }) + "\n" ) 
        sys.stdout.flush() 
        return

    try:
        request = json.loads(raw)
        result = handle(request)
    except Exception as error:
        result = {
            "requestId": request.get("requestId", "unknown")
            if "request" in locals() and isinstance(request, dict)
            else "unknown",
            "error": str(error),
        }

    sys.stdout.write(json.dumps(result) + "\n")
    sys.stdout.flush()


if __name__ == "__main__":
    main()
