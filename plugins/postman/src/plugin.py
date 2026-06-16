import glob
import json
import os
import sys
import tempfile

POSTMAN_DIR = os.path.expandvars(r"%APPDATA%\Postman")
PACKAGES_DIR = os.path.join(POSTMAN_DIR, "packages")


def send_response(response):
    print(json.dumps(response))
    sys.stdout.flush()


def is_postman_installed():
    return os.path.exists(POSTMAN_DIR)


def get_latest_package_dir():
    package_paths = glob.glob(os.path.join(PACKAGES_DIR, "app-*"))

    if not package_paths:
        return None

    package_paths.sort()

    return package_paths[-1]


def get_config_file():
    package_dir = get_latest_package_dir()

    if not package_dir:
        return None

    return os.path.join(package_dir, "settings.json")


def ensure_config_exists():
    config_file = get_config_file()

    if not config_file:
        return None

    os.makedirs(os.path.dirname(config_file), exist_ok=True)

    if not os.path.exists(config_file):
        with open(config_file, "w", encoding="utf-8") as file:
            json.dump({}, file)

    return config_file


def load_settings():
    config_file = ensure_config_exists()

    if not config_file:
        return {}

    with open(config_file, "r", encoding="utf-8") as file:
        content = file.read().strip()

        if not content:
            return {}

        return json.loads(content)


def save_settings(settings):
    config_file = ensure_config_exists()

    if not config_file:
        return

    fd, temp_path = tempfile.mkstemp(dir=os.path.dirname(config_file), suffix=".tmp")

    os.close(fd)

    with open(temp_path, "w", encoding="utf-8") as file:
        json.dump(settings, file, indent=2)

    os.replace(temp_path, config_file)


def deep_merge(original, updates):
    merged = original.copy()

    for key, value in updates.items():
        if key in merged and isinstance(merged[key], dict) and isinstance(value, dict):
            merged[key] = deep_merge(merged[key], value)
        else:
            merged[key] = value

    return merged


def handle_check_installed():
    return is_postman_installed()


def handle_apply(settings, dry_run, request_id):
    current_settings = load_settings()

    new_settings = deep_merge(current_settings, settings)

    changed = current_settings != new_settings

    if changed and not dry_run:
        save_settings(new_settings)

    return {"requestId": request_id, "changed": changed}


def main():
    try:
        raw_input = sys.stdin.read()

        request = json.loads(raw_input)

        request_id = request.get("requestId") or "unknown"

        command = request.get("command")

        args = request.get("args", {})

        dry_run = args.get("dryRun", False)

        settings = args.get("settings", {})

        if command == "check_installed":
            response = {"requestId": request_id, "installed": handle_check_installed()}

        elif command == "apply":
            response = handle_apply(settings, dry_run, request_id)

        else:
            response = {"requestId": request_id, "error": f"Unknown command: {command}"}

        send_response(response)

    except Exception as error:
        send_response({"requestId": "unknown", "error": str(error)})


if __name__ == "__main__":
    main()
