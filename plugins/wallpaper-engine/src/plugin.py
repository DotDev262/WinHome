import os
import json
import tempfile

def get_config_path():
    paths = [
        os.path.expandvars(
            r"%PROGRAMFILES(X86)%\Steam\steamapps\common\wallpaper_engine\config\config.json"
        ),
        os.path.expandvars(
            r"%PROGRAMFILES%\Steam\steamapps\common\wallpaper_engine\config\config.json"
        )
    ]

    for path in paths:
        if os.path.exists(path):
            return path

    return None 
def read_config():
    config_path = get_config_path()
    if not config_path:
        return {}

    with open(config_path, "r", encoding="utf-8") as f:
        data = json.load(f)

    return data

def save_config(config):
    config_path = get_config_path()

    directory = os.path.dirname(config_path)

    fd, temp_path = tempfile.mkstemp(dir=directory)

    try:
        with os.fdopen(fd, "w", encoding="utf-8") as f:
            json.dump(config, f, indent=4)

            # POSIX trailing newline
            f.write("\n")

        os.replace(temp_path, config_path)

        

    finally:
        if os.path.exists(temp_path):
            os.remove(temp_path)

def check_installed(args=None):
    return True

def deep_merge(base, updates):
    for key, value in updates.items():

        if (
            key in base
            and isinstance(base[key], dict)
            and isinstance(value, dict)
        ):
            deep_merge(base[key], value)

        else:
            base[key] = value

    return base

def apply(args):
    try:
        request_id = args.get("requestId")

        settings = args.get("settings", {})
        dry_run = args.get("dryRun", False)

        config = read_config()

        config = deep_merge(config, settings)

        if not dry_run:
            save_config(config)

        return {
            "success": True,
            "requestId": request_id,
            "dryRun": dry_run,
            "settings": config
        }

    except Exception as e:
        return {
            "success": False,
            "requestId": args.get("requestId"),
            "error": str(e)
        }
