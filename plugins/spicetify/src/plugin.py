import configparser
import json
import os
import shutil
import sys
import tempfile
import uuid

CONFIG_DIR = ".spicetify"
CONFIG_FILE = "config.ini"


def log(msg):
    sys.stderr.write(f"[spicetify-plugin] {msg}\n")
    sys.stderr.flush()


def backup_corrupt_config(file_path: str):
    if not os.path.exists(file_path):
        return

    backup_path = f"{file_path}.corrupt.{uuid.uuid4()}"

    try:
        shutil.copy2(file_path, backup_path)
        log(f"Backed up corrupt config to {backup_path}")
    except Exception as e:
        log(f"Failed to backup corrupt config: {e}")


def get_config_path():
    user_profile = os.getenv("USERPROFILE") or os.path.expanduser("~")
    return os.path.join(user_profile, CONFIG_DIR, CONFIG_FILE)


def read_ini(file_path: str) -> configparser.ConfigParser:
    config = configparser.ConfigParser()

    if not os.path.exists(file_path):
        return config

    try:
        with open(file_path, "r", encoding="utf-8") as f:
            config.read_file(f)

        return config

    except Exception:
        backup_corrupt_config(file_path)
        return configparser.ConfigParser()


def normalize_value(value):
    if isinstance(value, bool):
        return "1" if value else "0"

    if isinstance(value, (list, tuple)):
        return ",".join(str(item) for item in value)

    if value is None:
        return ""

    return str(value)


def merge_settings(config: configparser.ConfigParser, settings: dict) -> bool:
    changed = False

    for section, values in settings.items():
        if not isinstance(values, dict):
            continue

        if not config.has_section(section):
            config.add_section(section)
            changed = True

        for key, value in values.items():
            normalized = normalize_value(value)

            if not config.has_option(section, key) or config.get(section, key) != normalized:
                config.set(section, key, normalized)
                changed = True

    return changed


def write_ini(file_path: str, config: configparser.ConfigParser) -> None:
    dir_path = os.path.dirname(file_path)
    os.makedirs(dir_path, exist_ok=True)

    fd, temp_path = tempfile.mkstemp(prefix="spicetify-", dir=dir_path)

    try:
        with os.fdopen(fd, "w", encoding="utf-8", newline="\n") as f:
            config.write(f)

        os.replace(temp_path, file_path)
    except Exception:
        try:
            os.unlink(temp_path)
        except OSError:
            pass

        raise


def check_installed(args: dict, request_id: str) -> dict:
    user_profile = os.getenv("USERPROFILE") or os.path.expanduser("~")
    spicetify_dir = os.path.join(user_profile, CONFIG_DIR)

    installed = (
        os.path.isdir(spicetify_dir)
        or shutil.which("spicetify.exe") is not None
        or shutil.which("spicetify") is not None
    )

    return {
        "requestId": request_id,
        "success": True,
        "changed": False,
        "data": installed,
    }


def apply_config(args: dict, context: dict, request_id: str) -> dict:
    dry_run = context.get("dryRun", False)
    settings = args.get("settings", {})

    if not isinstance(settings, dict):
        return {
            "requestId": request_id,
            "success": False,
            "changed": False,
            "error": "settings must be an object",
            "data": None,
        }

    try:
        config_path = get_config_path()
        config = read_ini(config_path)
        changed = merge_settings(config, settings)

        if not changed:
            return {
                "requestId": request_id,
                "success": True,
                "changed": False,
                "data": None,
            }

        if dry_run:
            return {
                "requestId": request_id,
                "success": True,
                "changed": True,
                "data": {
                    "path": config_path,
                    "settings": settings,
                },
            }

        write_ini(config_path, config)

        return {
            "requestId": request_id,
            "success": True,
            "changed": True,
            "data": {
                "path": config_path,
            },
        }

    except Exception as e:
        log(f"Failed to apply config: {e}")

        return {
            "requestId": request_id,
            "success": False,
            "changed": False,
            "error": str(e),
            "data": None,
        }


def main():
    input_data = sys.stdin.read()

    if not input_data:
        response = {
            "requestId": "unknown",
            "success": False,
            "changed": False,
            "error": "No input provided on stdin",
            "data": None,
        }

        sys.stdout.write(json.dumps(response) + "\n")
        sys.stdout.flush()
        return

    try:
        request = json.loads(input_data)
    except Exception as e:
        response = {
            "requestId": "unknown",
            "success": False,
            "changed": False,
            "error": f"Failed to parse JSON request: {str(e)}",
            "data": None,
        }

        sys.stdout.write(json.dumps(response) + "\n")
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
            response = check_installed(args, request_id)
        elif command == "apply":
            response = apply_config(args, context, request_id)
        else:
            response["error"] = f"Unknown command: {command}"
    except Exception as fatal_err:
        response["error"] = f"Internal Script Error: {str(fatal_err)}"

    sys.stdout.write(json.dumps(response) + "\n")
    sys.stdout.flush()


if __name__ == "__main__":
    main()
