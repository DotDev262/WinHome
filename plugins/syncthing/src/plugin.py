import json
import os
import shutil
import sys
import tempfile
import xml.etree.ElementTree as ET
from pathlib import Path


def check_installed():
    """Check if syncthing.exe is installed and in the system PATH."""
    return shutil.which("syncthing.exe") is not None


def get_config_path():
    """Find the Syncthing config file path."""
    if os.name == "nt":
        base = os.environ.get("LOCALAPPDATA", "~\\AppData\\Local")
        return Path(base) / "Syncthing" / "config.xml"
    else:
        return Path("~/.config/syncthing/config.xml").expanduser()


def update_element(parent, tag, new_data):
    """Safely update an XML element."""
    changed = False
    elem = parent.find(tag)
    if elem is None:
        elem = ET.SubElement(parent, tag)
        changed = True

    for key, value in new_data.items():
        str_value = str(value).lower() if isinstance(value, bool) else str(value)

        child = elem.find(key)
        if child is not None:
            if child.text != str_value:
                child.text = str_value
                changed = True
        else:
            new_child = ET.SubElement(elem, key)
            new_child.text = str_value
            changed = True

    return changed


def process_command(request):
    """Handle the incoming JSON request."""
    request_id = request.get("requestId") or "unknown"
    command = request.get("command")
    args = request.get("args", {})

    if command == "check_installed":
        return {"requestId": request_id, "installed": check_installed()}

    elif command == "apply":
        dry_run = args.get("dryRun", False)
        config_path = get_config_path()

        if config_path.exists():
            tree = ET.parse(config_path)
            root = tree.getroot()
        else:
            root = ET.Element("configuration", version="37")
            tree = ET.ElementTree(root)

        changed = False
        settings = args.get("settings", {})

        if "gui" in settings and update_element(root, "gui", settings["gui"]):
            changed = True
        if "options" in settings and update_element(root, "options", settings["options"]):
            changed = True

        if changed and not dry_run:
            config_path.parent.mkdir(parents=True, exist_ok=True)

            fd, temp_path = tempfile.mkstemp(dir=config_path.parent, suffix=".xml")
            with os.fdopen(fd, "wb") as f:
                tree.write(f, encoding="utf-8", xml_declaration=True)

            os.replace(temp_path, config_path)

        return {"requestId": request_id, "changed": changed}

    else:
        return {"requestId": request_id, "error": f"Unknown command: {command}"}


def main():
    """Read JSON from the standard input."""
    try:
        input_data = sys.stdin.read().strip()
        if not input_data:
            print(json.dumps({"requestId": "unknown", "error": "No input received"}))
            return

        request = json.loads(input_data)
        response = process_command(request)
        print(json.dumps(response))

    except json.JSONDecodeError:
        print(json.dumps({"requestId": "unknown", "error": "Invalid JSON"}))
    except Exception as e:
        print(json.dumps({"requestId": "unknown", "error": str(e)}))


if __name__ == "__main__":
    main()
