import json
import os
import shutil
import sys
import tempfile
from configparser import ConfigParser


def check_installed(args: dict) -> dict:
    """Detects if Greenshot is installed by checking AppData directories or PATH execution contexts."""
    request_id = args.get("requestId", "unknown")
    app_data = os.environ.get("APPDATA", os.path.expanduser("~\\AppData\\Roaming"))
    config_dir = os.path.join(app_data, "Greenshot")
    
    installed = os.path.exists(config_dir) or (shutil.which("Greenshot.exe") is not None)
    return {"requestId": request_id, "installed": installed}


def apply(args: dict) -> dict:
    """Reads existing INI configuration, deep-merges nested settings, and handles atomic writes."""
    request_id = args.get("requestId", "unknown")
    settings = args.get("settings", {})
    dry_run = args.get("dryRun", False)

    app_data = os.environ.get("APPDATA", os.path.expanduser("~\\AppData\\Roaming"))
    config_dir = os.path.join(app_data, "Greenshot")
    config_path = os.path.join(config_dir, "Greenshot.ini")

    config = ConfigParser()
    config.optionxform = str  # type: ignore

    if os.path.exists(config_path):
        config.read(config_path, encoding="utf-8")

    for compound_key, value in settings.items():
        if "\\" in compound_key:
            section, key = compound_key.split("\\", 1)
        else:
            section, key = "General", compound_key

        if not config.has_section(section):
            config.add_section(section)

        if isinstance(value, bool):
            config.set(section, key, "True" if value else "False")
        else:
            config.set(section, key, str(value))

    if dry_run:
        return {"requestId": request_id, "changed": True}

    if not os.path.exists(config_dir):
        os.makedirs(config_dir, exist_ok=True)

    fd, temp_path = tempfile.mkstemp(dir=config_dir, text=True)
    try:
        with os.fdopen(fd, "w", encoding="utf-8") as f:
            config.write(f, space_around_delimiters=False)

        with open(temp_path, "a", encoding="utf-8") as f:
            f.write("\n")

        os.replace(temp_path, config_path)
    except Exception as e:
        if os.path.exists(temp_path):
            os.remove(temp_path)
        return {"requestId": request_id, "error": str(e), "changed": False}

    return {"requestId": request_id, "changed": True}


def main():
    """Main JSON-RPC routing entrypoint handling host engine input/output pipelines."""
    try:
        input_data = sys.stdin.read().strip()
        if not input_data:
            return
        
        request = json.loads(input_data)
        command = request.get("command")
        args = request.get("args", {})
        
        if command == "check_installed":
            response = check_installed(args)
        elif command == "apply":
            response = apply(args)
        else:
            response = {"requestId": args.get("requestId", "unknown"), "error": f"Unknown command: {command}"}
            
        sys.stdout.write(json.dumps(response) + "\n")
    except Exception as e:
        sys.stdout.write(json.dumps({"error": str(e)}) + "\n")


if __name__ == "__main__":
    main()
