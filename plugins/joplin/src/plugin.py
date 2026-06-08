import json
import os
import shutil
import sys


COMMON_PATHS = [
    os.path.expandvars(r"%LOCALAPPDATA%\Programs\Joplin\Joplin.exe"),
    os.path.expandvars(r"%ProgramFiles%\Joplin\Joplin.exe"),
    os.path.expandvars(r"%ProgramFiles(x86)%\Joplin\Joplin.exe"),
]

CONFIG_DIR = os.path.expandvars(r"%APPDATA%\joplin-desktop")
SETTINGS_FILE = os.path.join(CONFIG_DIR, "settings.json")


def send_response(response):
    print(json.dumps(response))
    sys.stdout.flush()


def is_joplin_installed():
    if shutil.which("joplin.exe"):
        return True

    if shutil.which("joplin-desktop.exe"):
        return True

    for path in COMMON_PATHS:
        if os.path.exists(path):
            return True

    return False


def ensure_config_exists():
    os.makedirs(CONFIG_DIR, exist_ok=True)

    if not os.path.exists(SETTINGS_FILE):
        with open(SETTINGS_FILE, "w", encoding="utf-8") as file:
            json.dump({}, file)


def load_settings():
    ensure_config_exists()

    with open(SETTINGS_FILE, "r", encoding="utf-8") as file:
        content = file.read().strip()

        if not content:
            return {}

        return json.loads(content)

def save_settings(settings):
    ensure_config_exists()

    with open(SETTINGS_FILE, "w", encoding="utf-8") as file:
        json.dump(settings, file, indent=2)


def handle_check_installed():
    installed = is_joplin_installed()

    return {
        "success": True,
        "data": {
            "installed": installed
        }
    }


def handle_apply(args, dry_run=False):
    current_settings = load_settings()

    new_settings = current_settings.copy()

    changed = False
    changes = {}

    for key, value in args.items():
        old_value = current_settings.get(key)

        if old_value != value:
            changed = True
            changes[key] = {
                "old": old_value,
                "new": value
            }

        new_settings[key] = value

    if changed and not dry_run:
        save_settings(new_settings)

    return {
        "success": True,
        "changed": changed,
        "dry_run": dry_run,
        "data": {
            "changes": changes
        }
    }


def main():
    try:
        raw_input = sys.stdin.read()
        request = json.loads(raw_input)

        command = request.get("command")
        args = request.get("args", {})
        dry_run = request.get("dry_run", False)

        if command == "check_installed":
            response = handle_check_installed()

        elif command == "apply":
            response = handle_apply(args, dry_run)

        else:
            response = {
                "success": False,
                "error": f"Unknown command: {command}"
            }

        send_response(response)

    except Exception as e:
        send_response({
            "success": False,
            "error": str(e)
        })


if __name__ == "__main__":
    main()