#!/usr/bin/env python3
import os
import sys
import json
import tempfile

def get_config_paths():
    program_files_x86 = os.environ.get("ProgramFiles(x86)", r"C:\Program Files (x86)")
    program_files_x64 = os.environ.get("ProgramFiles", r"C:\Program Files")
    return [
        os.path.join(program_files_x86, "Steam", "steamapps", "common", "wallpaper_engine", "config", "config.json"),
        os.path.join(program_files_x64, "Steam", "steamapps", "common", "wallpaper_engine", "config", "config.json")
    ]

def deep_merge(target, source):
    for key, value in source.items():
        if key in target and isinstance(target[key], dict) and isinstance(value, dict):
            deep_merge(target[key], value)
        else:
            target[key] = value
    return target

def check_installed(args):
    request_id = args.get("requestId", "unknown")
    installed = any(os.path.exists(path) for path in get_config_paths())
    print(json.dumps({"requestId": request_id, "status": "success", "installed": installed}))

def apply(args):
    request_id = args.get("requestId", "unknown")
    settings = args.get("settings", {})
    dry_run = args.get("dryRun", False)
    
    target_path = None
    for path in get_config_paths():
        if os.path.exists(path) or os.path.exists(os.path.dirname(path)):
            target_path = path
            break
    if not target_path:
        program_files_x86 = os.environ.get("ProgramFiles(x86)", r"C:\Program Files (x86)")
        target_path = os.path.join(program_files_x86, "Steam", "steamapps", "common", "wallpaper_engine", "config", "config.json")

    config_dir = os.path.dirname(target_path)
    existing_config = {}
    if os.path.exists(target_path):
        try:
            with open(target_path, "r", encoding="utf-8") as f:
                existing_config = json.load(f)
        except Exception:
            existing_config = {}

    updated_config = deep_merge(existing_config, settings)
    if dry_run:
        print(json.dumps({"requestId": request_id, "status": "success", "dryRun": True, "path": target_path}))
        return

    try:
        if not os.path.exists(config_dir):
            os.makedirs(config_dir, exist_ok=True)
        fd, temp_path = tempfile.mkstemp(dir=config_dir, suffix=".tmp")
        try:
            with os.fdopen(fd, "w", encoding="utf-8") as tmp_file:
                json.dump(updated_config, tmp_file, indent=4)
                tmp_file.write("\n")
            os.replace(temp_path, target_path)
        except Exception as e:
            if os.path.exists(temp_path):
                os.remove(temp_path)
            raise e
        print(json.dumps({"requestId": request_id, "status": "success", "path": target_path}))
    except Exception as err:
        print(json.dumps({"requestId": request_id, "status": "error", "message": str(err)}))

def main():
    try:
        input_data = sys.stdin.read().strip()
        if not input_data:
            sys.exit(1)
        args = json.loads(input_data)
    except Exception:
        sys.exit(1)
    action = args.get("action")
    if action == "check_installed":
        check_installed(args)
    elif action == "apply":
        apply(args)
    else:
        sys.exit(1)

if __name__ == "__main__":
    main()
