import json
import os
import shutil
import sys
import uuid
import xml.etree.ElementTree as ET

CHOCO_NS = "http://chocolatey.org/schema/chocolatey-configuration"
ET.register_namespace("", CHOCO_NS)


def log(msg):
    sys.stderr.write(f"[chocolatey-plugin] {msg}\n")
    sys.stderr.flush()


def get_config_path():
    choco_install = os.getenv("ChocolateyInstall")
    if not choco_install:
        program_data = os.getenv("ALLUSERSPROFILE", "C:\\ProgramData")
        choco_install = os.path.join(program_data, "chocolatey")
    return os.path.join(choco_install, "config", "chocolatey.config")


def read_xml(file_path):
    if not os.path.exists(file_path):
        root = ET.Element(f"{{{CHOCO_NS}}}chocolatey")
        ET.SubElement(root, f"{{{CHOCO_NS}}}config")
        ET.SubElement(root, f"{{{CHOCO_NS}}}features")
        return ET.ElementTree(root)
    try:
        return ET.parse(file_path)
    except Exception as e:
        backup_path = f"{file_path}.corrupted.{uuid.uuid4().hex[:8]}.bak"
        try:
            shutil.copy2(file_path, backup_path)
            log(
                f"Warning: could not parse {file_path}: {e}. Backed up to {backup_path}. Starting with default."
            )
        except Exception:
            log(
                f"Warning: could not parse {file_path}: {e}. Starting with default."
            )
        root = ET.Element(f"{{{CHOCO_NS}}}chocolatey")
        ET.SubElement(root, f"{{{CHOCO_NS}}}config")
        ET.SubElement(root, f"{{{CHOCO_NS}}}features")
        return ET.ElementTree(root)


def write_xml(file_path, tree):
    os.makedirs(os.path.dirname(file_path), exist_ok=True)
    tmp = file_path + ".tmp"
    tree.write(tmp, encoding="utf-8", xml_declaration=True)
    os.replace(tmp, file_path)


def merge_settings(tree, source):
    changed = False
    root = tree.getroot()

    if "config" in source and isinstance(source["config"], dict):
        config_el = root.find(f"{{{CHOCO_NS}}}config")
        for key, value in source["config"].items():
            if value is None:
                continue

            str_val = (
                str(value)
                if not isinstance(value, bool)
                else ("true" if value else "false")
            )

            if config_el is None:
                config_el = ET.SubElement(root, f"{{{CHOCO_NS}}}config")
                changed = True

            existing = None
            for add_el in config_el.findall(f"{{{CHOCO_NS}}}add"):
                if add_el.get("key") == key:
                    existing = add_el
                    break

            if existing is not None:
                if existing.get("value") != str_val:
                    existing.set("value", str_val)
                    changed = True
            else:
                ET.SubElement(
                    config_el,
                    f"{{{CHOCO_NS}}}add",
                    {"key": key, "value": str_val},
                )
                changed = True

    if "features" in source and isinstance(source["features"], dict):
        features_el = root.find(f"{{{CHOCO_NS}}}features")
        for name, enabled in source["features"].items():
            if enabled is None:
                continue

            str_enabled = "true" if enabled else "false"

            if features_el is None:
                features_el = ET.SubElement(root, f"{{{CHOCO_NS}}}features")
                changed = True

            existing = None
            for feature_el in features_el.findall(f"{{{CHOCO_NS}}}feature"):
                if feature_el.get("name") == name:
                    existing = feature_el
                    break

            if existing is not None:
                if existing.get("enabled") != str_enabled:
                    existing.set("enabled", str_enabled)
                    changed = True
            else:
                ET.SubElement(
                    features_el,
                    f"{{{CHOCO_NS}}}feature",
                    {"name": name, "enabled": str_enabled},
                )
                changed = True

    return changed


def check_installed(args, request_id):
    installed = (
        shutil.which("choco.exe") is not None
        or shutil.which("choco") is not None
    )
    return {
        "requestId": request_id,
        "success": True,
        "changed": False,
        "data": installed,
    }


def apply_config(args, context, request_id):
    dry_run = context.get("dryRun", False)

    try:
        config_path = get_config_path()
        tree = read_xml(config_path)

        settings = args.get("settings", {})
        changed = merge_settings(tree, settings)

        if not changed:
            return {
                "requestId": request_id,
                "success": True,
                "changed": False,
            }

        if dry_run:
            log(f"Would update Chocolatey config at {config_path}")
            return {
                "requestId": request_id,
                "success": True,
                "changed": True,
            }

        write_xml(config_path, tree)
        log(f"Updated Chocolatey config: {config_path}")

        return {
            "requestId": request_id,
            "success": True,
            "changed": True,
        }

    except Exception as e:
        log(f"Failed to apply config: {e}")
        return {
            "requestId": request_id,
            "success": False,
            "changed": False,
            "error": str(e),
        }


def main():
    input_data = sys.stdin.read()
    if not input_data:
        return

    try:
        request = json.loads(input_data)
    except Exception as e:
        response = {
            "requestId": "unknown",
            "success": False,
            "changed": False,
            "error": f"Failed to parse request: {e}",
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
