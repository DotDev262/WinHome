import configparser
import json
import os
import shutil
import sys
import tempfile
import uuid


def check_installed(request_id: str) -> dict:
    installed = False

    # Check in PATH
    if shutil.which("Rainmeter.exe") or shutil.which("Rainmeter"):
        installed = True
    else:
        # Check standard installation path
        program_files = os.getenv("PROGRAMFILES", "C:\\Program Files")
        if os.path.exists(os.path.join(program_files, "Rainmeter", "Rainmeter.exe")):
            installed = True

    return {"requestId": request_id, "success": True, "changed": False, "data": installed}


def apply_config(args: dict, context: dict, request_id: str) -> dict:
    dry_run = context.get("dryRun", False)
    settings = args.get("settings", {})

    if not isinstance(settings, dict):
        return {
            "requestId": request_id,
            "success": False,
            "changed": False,
            "error": "settings must be a dictionary",
        }

    appdata = os.getenv("APPDATA")
    if not appdata:
        return {
            "requestId": request_id,
            "success": False,
            "changed": False,
            "error": "APPDATA environment variable not found",
        }

    config_dir = os.path.join(appdata, "Rainmeter")
    config_file = os.path.join(config_dir, "Rainmeter.ini")

    # We use configparser with optionxform=str to preserve case
    parser = configparser.ConfigParser(interpolation=None)
    parser.optionxform = str

    if os.path.exists(config_file):
        try:
            # Try parsing to make sure it's valid INI
            parser.read(config_file, encoding="utf-8")
        except Exception:
            # If the config file is corrupt, back it up using UUID
            backup_file = f"{config_file}.{uuid.uuid4().hex}.bak"
            shutil.copy2(config_file, backup_file)
            # Reinitialize empty parser, we will rewrite from scratch
            parser = configparser.ConfigParser(interpolation=None)
            parser.optionxform = str

    # Track changes
    changed = False

    # Merge new settings
    for section, keys in settings.items():
        if not isinstance(keys, dict):
            continue

        if not parser.has_section(section):
            parser.add_section(section)
            changed = True

        for key, value in keys.items():
            str_val = str(value)
            if not parser.has_option(section, key) or parser.get(section, key) != str_val:
                parser.set(section, key, str_val)
                changed = True

    if not changed:
        return {"requestId": request_id, "success": True, "changed": False}

    if dry_run:
        return {"requestId": request_id, "success": True, "changed": True}

    try:
        os.makedirs(config_dir, exist_ok=True)
        fd, tmp_path = tempfile.mkstemp(dir=config_dir, prefix="Rainmeter.ini.tmp")
        try:
            with os.fdopen(fd, "w", encoding="utf-8") as f:
                parser.write(f, space_around_delimiters=False)
            os.replace(tmp_path, config_file)
        except Exception as e:
            os.unlink(tmp_path)
            raise e

        return {"requestId": request_id, "success": True, "changed": True}
    except Exception as e:
        return {
            "requestId": request_id,
            "success": False,
            "changed": False,
            "error": f"Failed to apply config: {e}",
        }


def main():
    input_data = sys.stdin.read()
    if not input_data:
        return

    try:
        request = json.loads(input_data)
    except Exception as e:
        sys.stdout.write(
            json.dumps(
                {
                    "requestId": "unknown",
                    "success": False,
                    "changed": False,
                    "error": f"Invalid JSON: {e}",
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

    if command == "check_installed":
        response = check_installed(request_id)
    elif command == "apply":
        response = apply_config(args, context, request_id)
    else:
        response = {
            "requestId": request_id,
            "success": False,
            "changed": False,
            "error": f"Unknown command: {command}",
        }

    sys.stdout.write(json.dumps(response) + "\n")
    sys.stdout.flush()


if __name__ == "__main__":
    main()
