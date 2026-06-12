#!/usr/bin/env python3
import json
import os
import sys
import tempfile


def get_config_paths():
    program_files_x86 = os.environ.get("ProgramFiles(x86)", r"C:\Program Files (x86)")
    program_files_x64 = os.environ.get("ProgramFiles", r"C:\Program Files")
    return [
        os.path.join(program_files_x86, "Steam", "steamapps", "common", "wallpaper_engine", "config", "config.json"),
        os.path.join(program_files_x64, "Steam", "steamapps", "common", "wallpaper_engine", "config", "config.json"),
    ]


def deep_merge(target, source):
    """Recursively deep merges source into target and returns True if any value changed."""
    any_changed = False
    for key, value in source.items():
        if key in target and isinstance(target[key], dict) and isinstance(value, dict):
            if deep_merge(target[key], value):
                any_changed = True
        else:
            if key not in target or target[key] != value:
                target[key] = value
                any_changed = True
    return any_changed


def check_installed() -> bool:
    """Strictly returns a bare boolean indicating system installation state."""
    return any(os.path.exists(path) for path in get_config_paths())


def apply(request_id, args):
    settings = args.get("settings", {})
    dry_run = args.get("dryRun", False)

    if not isinstance(settings, dict):
        return {"requestId": request_id, "error": "settings must be a dictionary"}

    target_path = None
    for path in get_config_paths():
        if os.path.exists(path) or os.path.exists(os.path.dirname(path)):
            target_path = path
            break
    if not target_path:
        program_files_x86 = os.environ.get("ProgramFiles(x86)", r"C:\Program Files (x86)")
        target_path = os.path.join(
            program_files_x86, "Steam", "steamapps", "common", "wallpaper_engine", "config", "config.json"
        )

    config_dir = os.path.dirname(target_path)
    existing_config = {}
    if os.path.exists(target_path):
        try:
            with open(target_path, "r", encoding="utf-8") as f:
                existing_config = json.load(f)
        except Exception:
            existing_config = {}

    has_changes = deep_merge(existing_config, settings)

    if dry_run:
        return {"requestId": request_id, "dryRun": True, "changed": has_changes, "path": target_path}

    if not has_changes:
        return {"requestId": request_id, "changed": False, "path": target_path}

    try:
        if not os.path.exists(config_dir):
            os.makedirs(config_dir, exist_ok=True)
        fd, temp_path = tempfile.mkstemp(dir=config_dir, suffix=".tmp")
        try:
            with os.fdopen(fd, "w", encoding="utf-8") as tmp_file:
                json.dump(existing_config, tmp_file, indent=4)
                tmp_file.write("\n")
            os.replace(temp_path, target_path)
        except Exception as e:
            if os.path.exists(temp_path):
                os.remove(temp_path)
            raise e
        return {"requestId": request_id, "changed": True, "path": target_path}
    except Exception as err:
        return {"requestId": request_id, "error": str(err)}


def main():
    request_id = "unknown"
    try:
        input_data = sys.stdin.read().strip()
        if not input_data:
            print(json.dumps({"requestId": request_id, "error": "No input received"}))
            return
        request = json.loads(input_data)
    except Exception as e:
        print(json.dumps({"requestId": request_id, "error": f"Invalid JSON: {str(e)}"}))
        return

    request_id = request.get("requestId") or "unknown"
    command = request.get("command")
    args = request.get("args", {})

    if command == "check_installed":
        print(json.dumps({"requestId": request_id, "installed": check_installed()}))
    elif command == "apply":
        result = apply(request_id, args)
        print(json.dumps(result))
    else:
        print(json.dumps({"requestId": request_id, "error": f"Unknown command: {command}"}))


if __name__ == "__main__":
    main()
