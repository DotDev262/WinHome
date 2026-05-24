# /// script
# dependencies = [
#   "pyyaml",
# ]
# ///

import json
import os
import shutil
import sys

import yaml


def get_config_path() -> str:
    return os.path.join(os.environ.get("APPDATA", ""), "GitHub CLI", "config.yml")


def log(message: str) -> None:
    sys.stderr.write(f"[gh-plugin] {message}\n")
    sys.stderr.flush()


def read_yaml(file_path: str) -> dict:
    if not os.path.exists(file_path):
        return {}

    with open(file_path, "r", encoding="utf-8") as file_handle:
        data = yaml.safe_load(file_handle)
        return data if isinstance(data, dict) else {}


def write_yaml(file_path: str, data: dict) -> None:
    os.makedirs(os.path.dirname(file_path), exist_ok=True)
    with open(file_path, "w", encoding="utf-8") as file_handle:
        yaml.dump(data, file_handle, default_flow_style=False, sort_keys=False)


def merge_settings(target: dict, source: dict) -> bool:
    changed = False

    for key, value in source.items():
        if value == "":
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


def check_installed() -> dict:
    installed = shutil.which("gh") is not None or shutil.which("gh.exe") is not None
    return {
        "success": True,
        "data": {
            "installed": installed,
        },
    }


def apply_config(args: dict) -> dict:
    dry_run = bool(args.get("dry_run", False))
    updates = {key: value for key, value in args.items() if key != "dry_run"}

    config_path = get_config_path()
    current_config = read_yaml(config_path)
    target = get_config_target(current_config)
    changed = merge_settings(target, updates)

    if dry_run:
        if changed:
            log(f"dry_run: would update {config_path}")
        else:
            log(f"dry_run: no changes for {config_path}")
        return {
            "success": True,
            "changed": changed,
        }

    if changed:
        write_yaml(config_path, current_config)

    return {
        "success": True,
        "changed": changed,
    }


def handle(request: dict) -> dict:
    command = request.get("command")
    args = request.get("args", {})

    if command == "check_installed":
        return check_installed()
    if command == "apply":
        if not isinstance(args, dict):
            raise ValueError("args must be an object")
        return apply_config(args)

    raise ValueError(f"Unknown command: {command}")


def main() -> None:
    raw = sys.stdin.readline()
    if not raw:
        return

    try:
        request = json.loads(raw)
        result = handle(request)
    except Exception as error:
        result = {
            "success": False,
            "error": str(error),
        }

    sys.stdout.write(json.dumps(result) + "\n")
    sys.stdout.flush()


if __name__ == "__main__":
    main()
