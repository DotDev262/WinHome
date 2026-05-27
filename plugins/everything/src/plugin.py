import sys
import json
import os
import configparser
from pathlib import Path


# ----------------------------
# PATH CONFIG
# ----------------------------
APPDATA = os.environ.get("APPDATA", "")
EVERYTHING_DIR = Path(APPDATA) / "Everything"
INI_PATH = EVERYTHING_DIR / "Everything.ini"


# ----------------------------
# CHECK INSTALLATION
# ----------------------------
def handle_check_installed():
    """
    Checks whether Everything config directory exists
    """
    installed = EVERYTHING_DIR.exists()
    return {"installed": installed}


# ----------------------------
# LOAD CONFIG
# ----------------------------
def load_config():
    config = configparser.ConfigParser()

    # IMPORTANT: preserve case sensitivity
    config.optionxform = str # type: ignore[attr-defined]

    if INI_PATH.exists():
        config.read(INI_PATH)

    return config


# ----------------------------
# MERGE CONFIG (deep-ish merge)
# ----------------------------
def merge_config(config, new_data):
    changed = False

    for section, values in new_data.items():
        if section not in config:
            config[section] = {}

        for key, value in values.items():
            str_value = str(value).lower() if isinstance(value, bool) else str(value)

            if config[section].get(key) != str_value:
                config[section][key] = str_value
                changed = True

    return changed


# ----------------------------
# APPLY CONFIG
# ----------------------------
def handle_apply(args):
    dry_run = args.get("dry_run", False)
    new_config = args.get("args", {})

    config = load_config()

    # preview (for dry-run output)
    preview = {
        section: {
            k: (str(v).lower() if isinstance(v, bool) else str(v))
            for k, v in values.items()
        }
        for section, values in new_config.items()
    }

    changed = merge_config(config, new_config)

    # DRY RUN → DO NOT WRITE FILE
    if dry_run:
        print(json.dumps({
            "success": True,
            "changed": changed,
            "dry_run": True,
            "preview": preview
        }), flush=True)
        return None

    # ensure directory exists
    EVERYTHING_DIR.mkdir(parents=True, exist_ok=True)

    # write INI file
    with open(INI_PATH, "w", encoding="utf-8") as f:
        config.write(f)

    return {
        "success": True,
        "changed": changed
    }


# ----------------------------
# MAIN ENTRYPOINT (STDIO)
# ----------------------------
def main():
    try:
        raw = sys.stdin.read().strip()

        if not raw:
            print(json.dumps({
                "success": False,
                "error": "empty input"
            }), flush=True)
            return

        payload = json.loads(raw)

        command = payload.get("command")
        args = payload.get("args", {})

        if command == "check_installed":
            result = handle_check_installed()

        elif command == "apply":
            result = handle_apply(args)

            # dry-run already printed
            if result is None:
                return

        else:
            result = {
                "success": False,
                "error": f"unknown command: {command}"
            }

        # IMPORTANT: ALWAYS PRINT JSON
        print(json.dumps(result), flush=True)

    except Exception as e:
        print(json.dumps({
            "success": False,
            "error": str(e)
        }), flush=True)


# ----------------------------
# ENTRYPOINT GUARANTEE
# ----------------------------
if __name__ == "__main__":
    main()