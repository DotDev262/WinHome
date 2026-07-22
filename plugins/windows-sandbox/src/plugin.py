import json
import os
import sys
import tempfile
import xml.etree.ElementTree as ET


def create_default_wsb():
    root = ET.Element("Configuration")

    ET.SubElement(root, "VGpu").text = "Enable"
    ET.SubElement(root, "Networking").text = "Enable"
    ET.SubElement(root, "AudioInput").text = "Enable"
    ET.SubElement(root, "VideoInput").text = "Enable"
    ET.SubElement(root, "ProtectedClient").text = "Disable"
    ET.SubElement(root, "PrinterRedirection").text = "Enable"
    ET.SubElement(root, "ClipboardRedirection").text = "Enable"
    ET.SubElement(root, "MemoryInMB").text = "1024"

    return root


def get_config_path():
    userprofile = os.environ.get("USERPROFILE")

    if not userprofile:
        raise Exception("USERPROFILE environment variable not found")

    documents_dir = os.path.join(userprofile, "Documents")
    os.makedirs(documents_dir, exist_ok=True)

    return os.path.join(documents_dir, "sandbox.wsb")


def read_xml(path: str):
    if not os.path.exists(path):
        return create_default_wsb()

    tree = ET.parse(path)
    return tree.getroot()


def write_xml(path: str, root: ET.Element) -> None:
    dir_name = os.path.dirname(path)

    fd, temp_path = tempfile.mkstemp(dir=dir_name, suffix=".wsb")

    try:
        with os.fdopen(fd, "w", encoding="utf-8", newline="\n") as f:
            tree = ET.ElementTree(root)
            tree.write(f, encoding="unicode", xml_declaration=True)

        os.replace(temp_path, path)

    except Exception:
        if os.path.exists(temp_path):
            os.remove(temp_path)
        raise


def check_installed() -> bool:
    windir = os.environ.get("WINDIR", r"C:\Windows")

    sandbox_path = os.path.join(windir, "System32", "WindowsSandbox.exe")

    return os.path.exists(sandbox_path)


def merge_settings(root: ET.Element, settings: dict) -> bool:
    changed = False

    bool_settings = {
        "vGPU": "VGpu",
        "networking": "Networking",
        "audioInput": "AudioInput",
        "videoInput": "VideoInput",
        "protectedClient": "ProtectedClient",
        "printerRedirection": "PrinterRedirection",
        "clipboardRedirection": "ClipboardRedirection",
    }

    # boolean fields
    for key, xml_tag in bool_settings.items():
        if key in settings:
            value = "Enable" if settings[key] else "Disable"

            node = root.find(xml_tag)
            if node is None:
                node = ET.SubElement(root, xml_tag)

            if node.text != value:
                node.text = value
                changed = True

    # memory field
    if "memoryInMB" in settings:
        value = str(settings["memoryInMB"])

        node = root.find("MemoryInMB")
        if node is None:
            node = ET.SubElement(root, "MemoryInMB")

        if node.text != value:
            node.text = value
            changed = True

    # mappedFolders support
    if "mappedFolders" in settings:
        new_value = settings["mappedFolders"]

        node = root.find("MappedFolders")
        existing = []

        if node is not None:
            for mf in node.findall("MappedFolder"):
                host = mf.find("HostFolder")
                readonly = mf.find("ReadOnly")

                existing.append(
                    {
                        "hostFolder": host.text if host is not None else "",
                        "readOnly": (
                            readonly.text.lower() == "true" if readonly is not None and readonly.text else False
                        ),
                    }
                )

        # only mark changed if different
        if existing != new_value:
            changed = True

        # rebuild XML safely
        if node is None:
            node = ET.SubElement(root, "MappedFolders")
        else:
            node.clear()

        for folder in new_value:
            mf = ET.SubElement(node, "MappedFolder")

            host = ET.SubElement(mf, "HostFolder")
            host.text = folder.get("hostFolder", "")

            readonly = ET.SubElement(mf, "ReadOnly")
            readonly.text = str(folder.get("readOnly", False)).lower()

    return changed


def apply_config(args: dict, context: dict, request_id: str) -> dict:
    dry_run = args.get("dryRun", False)
    settings = args.get("settings", {})

    if not isinstance(settings, dict):
        return {
            "requestId": request_id,
            "error": "settings must be a dictionary",
        }

    try:
        config_path = get_config_path()
        root = read_xml(config_path)

        changed = merge_settings(root, settings)

        if dry_run:
            return {
                "requestId": request_id,
                "changed": changed,
            }

        if changed:
            write_xml(config_path, root)

        return {
            "requestId": request_id,
            "changed": changed,
        }

    except Exception as e:
        return {
            "requestId": request_id,
            "error": str(e),
        }


def main():
    input_data = sys.stdin.read()

    if not input_data:
        response = {
            "requestId": "unknown",
            "error": "No input received",
        }
        sys.stdout.write(json.dumps(response) + "\n")
        return

    try:
        request = json.loads(input_data)
    except Exception as e:
        response = {
            "requestId": "unknown",
            "error": f"Invalid JSON: {e}",
        }
        sys.stdout.write(json.dumps(response) + "\n")
        return

    request_id = request.get("requestId") or "unknown"
    command = request.get("command")
    args = request.get("args", {})

    if command == "check_installed":
        installed = check_installed()

        response = {"requestId": request_id, "installed": installed}

    elif command == "apply":
        response = apply_config(args, request.get("context", {}), request_id)

    else:
        response = {
            "requestId": request_id,
            "error": f"Unknown command: {command}",
        }

    sys.stdout.write(json.dumps(response) + "\n")
    sys.stdout.flush()


if __name__ == "__main__":
    main()
