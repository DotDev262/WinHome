# /// script
# dependencies = [
#   "pyyaml",
# ]
# ///

import copy
import json
import os
import shutil
import sys
import tempfile
import uuid

import yaml

PLUGIN_NAME = "yasb"


def log(message: str) -> None:
    sys.stderr.write(f"[{PLUGIN_NAME}-plugin] {message}\n")
    sys.stderr.flush()


def get_user_profile() -> str:
    user_profile = os.getenv("USERPROFILE") or os.getenv("HOME")

    if not user_profile:
        raise Exception("USERPROFILE environment variable not found")

    return user_profile


def get_config_dir() -> str:
    return os.path.join(get_user_profile(), ".config", "yasb")


def get_config_path() -> str:
    return os.path.join(get_config_dir(), "config.yaml")


def read_yaml(file_path: str) -> dict:
    if not os.path.exists(file_path):
        return {}

    try:
        with open(file_path, "r", encoding="utf-8") as file_handle:
            data = yaml.safe_load(file_handle)
            return data if isinstance(data, dict) else {}
    except Exception as error:
        backup_path = f"{file_path}.{uuid.uuid4().hex}.bak"

        try:
            shutil.copy2(file_path, backup_path)
            log(f"Warning: could not parse {file_path}: {error}. Backed up to {backup_path} and starting fresh.")
        except Exception as backup_error:
            log(f"Warning: could not parse {file_path}: {error}. Failed to back it up: {backup_error}. Starting fresh.")

        return {}


def write_yaml(file_path: str, data: dict) -> None:
    os.makedirs(os.path.dirname(file_path), exist_ok=True)

    temp_fd, temp_path = tempfile.mkstemp(prefix="yasb-", dir=os.path.dirname(file_path))
    try:
        with os.fdopen(temp_fd, "w", encoding="utf-8") as file_handle:
            yaml.safe_dump(data, file_handle, default_flow_style=False, sort_keys=False)

        os.replace(temp_path, file_path)
    finally:
        if os.path.exists(temp_path):
            try:
                os.remove(temp_path)
            except OSError:
                pass


def merge_settings(target: dict, source: dict) -> bool:
    """Deep-merge `source` into `target`.

    Behavior:
    - Dict values are merged recursively.
    - Scalar values are overwritten when different or missing.

    Returns True if `target` was modified.
    """
    changed = False

    for key, value in source.items():
        if isinstance(value, dict):
            if key not in target or not isinstance(target[key], dict):
                target[key] = {}
                changed = True

            if merge_settings(target[key], value):
                changed = True
        else:
            if key not in target or target[key] != value:
                target[key] = value
                changed = True

    return changed


def check_installed(request_id: str) -> dict:
    installed = (
        shutil.which("yasb") is not None or shutil.which("yasb.exe") is not None or os.path.isdir(get_config_dir())
    )

    return {
        "requestId": request_id,
        "success": True,
        "changed": False,
        "data": installed,
    }


def apply_config(request_id: str, args: dict, context: dict) -> dict:
    dry_run = bool(context.get("dryRun", False))

    if not isinstance(args, dict):
        raise ValueError("args must be an object")

    settings = args.get("settings", {})

    if not isinstance(settings, dict):
        raise ValueError("settings must be an object")

    config_path = get_config_path()
    current_config = read_yaml(config_path)

    # Securely create a deep copy to evaluate alterations cleanly
    updated_config = copy.deepcopy(current_config)
    changed = merge_settings(updated_config, settings)

    if dry_run:
        log(f"Would update {config_path}" if changed else f"No changes for {config_path}")
        return {
            "requestId": request_id,
            "success": True,
            "changed": changed,
            "data": {"wouldChange": changed},
        }

    if changed:
        # Write the mutated copy
        write_yaml(config_path, updated_config)
        log(f"Updated yasb config: {config_path}")

    return {
        "requestId": request_id,
        "success": True,
        "changed": changed,
        "data": None,
    }


def main() -> None:
    input_data = sys.stdin.read()

    if not input_data:
        sys.stdout.write(
            json.dumps(
                {
                    "requestId": "unknown",
                    "success": False,
                    "changed": False,
                    "data": None,
                    "error": "Failed to parse request: empty stdin",
                }
            )
            + "\n"
        )
        sys.stdout.flush()
        return

    try:
        request = json.loads(input_data)
    except Exception as error:
        sys.stdout.write(
            json.dumps(
                {
                    "requestId": "unknown",
                    "success": False,
                    "changed": False,
                    "data": None,
                    "error": f"Failed to parse request: {error}",
                }
            )
            + "\n"
        )
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
            response = check_installed(request_id)
        elif command == "apply":
            response = apply_config(request_id, args, context)
        else:
            response["error"] = f"Unknown command: {command}"
    except Exception as fatal_error:
        response["error"] = f"Internal Script Error: {str(fatal_error)}"

    sys.stdout.write(json.dumps(response) + "\n")
    sys.stdout.flush()


if __name__ == "__main__":
    main()
