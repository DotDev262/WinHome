import configparser
import json
import os
import sys
from pathlib import Path


APPDATA = os.environ.get("APPDATA", "")
EVERYTHING_DIR = Path(APPDATA) / "Everything"
INI_PATH = EVERYTHING_DIR / "Everything.ini"


def handle_check_installed(request_id):
    return {
        "requestId": request_id,
        "success": True,
        "changed": False,
        "data": EVERYTHING_DIR.exists()
    }

def load_config():
    config = configparser.ConfigParser()
    config.optionxform = str  # type: ignore[attr-defined]

    if INI_PATH.exists():
        config.read(INI_PATH, encoding="utf-8")

    return config

def merge_config(config, settings):
    changed = False

    for section, values in settings.items():
        if not config.has_section(section):
            config.add_section(section)

        for key, value in values.items():
            value_str = str(value).lower()

            if config[section].get(key) != value_str:
                config[section][key] = value_str
                changed = True

    return changed

def handle_apply(args, context, request_id):
    dry_run = context.get("dryRun", False)
    settings = args.get("settings", {})

    config = load_config()

    changed = merge_config(config, settings)

    if dry_run:
        preview = {
            section: dict(config[section])
            for section in config.sections()
        }

        return {
            "requestId": request_id,
            "success": True,
            "changed": changed,
            "data": {
                "dryRun": True,
                "preview": preview
            }
        }

    EVERYTHING_DIR.mkdir(parents=True, exist_ok=True)

    with open(INI_PATH, "w", encoding="utf-8") as file:
        config.write(file)

    return {
        "requestId": request_id,
        "success": True,
        "changed": changed,
        "data": {}
    }

def main():
    try:
        raw = sys.stdin.read().strip()

        if not raw:
            print(json.dumps({
                "requestId": None,
                "success": False,
                "changed": False,
                "data": None,
                "error": "empty input"
            }))
            return

        payload = json.loads(raw)

        request_id = payload.get("requestId")
        command = payload.get("command")
        args = payload.get("args", {})
        context = payload.get("context", {})

        if command == "check_installed":
            result = handle_check_installed(request_id)

        elif command == "apply":
            result = handle_apply(args, context, request_id)

        else:
            result = {
                "requestId": request_id,
                "success": False,
                "error": f"unknown command: {command}"
            }

        print(json.dumps(result), flush=True)

    except Exception as error:
        print(json.dumps({
            "requestId": None,
            "success": False,
            "error": str(error)
        }), flush=True)

if __name__ == "__main__":
    main()

