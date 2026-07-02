import json
import os
import shutil
import sys
import tempfile
import uuid
import xml.etree.ElementTree as ET


def log(msg):
    sys.stderr.write(f"[nuget-plugin] {msg}\n")
    sys.stderr.flush()


def get_config_path():
    appdata = os.getenv("APPDATA", os.path.expanduser("~"))
    return os.path.join(appdata, "NuGet", "NuGet.Config")


def read_xml(file_path):
    if not os.path.exists(file_path):
        root = ET.Element("configuration")
        ET.SubElement(root, "packageSources")
        ET.SubElement(root, "disabledPackageSources")
        ET.SubElement(root, "fallbackPackageSources")
        ET.SubElement(root, "apikeys")
        ET.SubElement(root, "config")
        return ET.ElementTree(root)
    try:
        return ET.parse(file_path)
    except Exception as e:
        backup_path = f"{file_path}.corrupted.{uuid.uuid4().hex}.bak"
        try:
            shutil.copy2(file_path, backup_path)
            log(f"Warning: could not parse {file_path}: {e}. Backed up to {backup_path}.")
        except Exception:
            log(f"Warning: could not parse {file_path}: {e}. Starting with default.")
        root = ET.Element("configuration")
        ET.SubElement(root, "packageSources")
        ET.SubElement(root, "disabledPackageSources")
        ET.SubElement(root, "fallbackPackageSources")
        ET.SubElement(root, "apikeys")
        ET.SubElement(root, "config")
        return ET.ElementTree(root)


def write_xml(file_path, tree):
    os.makedirs(os.path.dirname(file_path), exist_ok=True)
    fd, tmp = tempfile.mkstemp(dir=os.path.dirname(file_path))
    os.close(fd)
    tree.write(tmp, encoding="utf-8", xml_declaration=True)
    os.replace(tmp, file_path)


def get_or_create(root, tag):
    el = root.find(tag)
    if el is None:
        el = ET.SubElement(root, tag)
    return el


def merge_settings(tree, settings):
    changed = False
    root = tree.getroot()

    if not isinstance(settings, dict):
        return changed

    sources = settings.get("packageSources")
    if isinstance(sources, list):
        el = get_or_create(root, "packageSources")
        existing = {a.get("key"): a for a in el.findall("add")}
        for src in sources:
            if not isinstance(src, dict):
                continue
            name = src.get("name")
            source = src.get("source")
            if not name or not source:
                continue
            if name in existing:
                if existing[name].get("value") != source:
                    existing[name].set("value", source)
                    changed = True
            else:
                ET.SubElement(el, "add", {"key": name, "value": source})
                changed = True

    api_keys = settings.get("apiKeys")
    if isinstance(api_keys, list):
        el = get_or_create(root, "apikeys")
        existing = {a.get("key"): a for a in el.findall("add")}
        for entry in api_keys:
            if not isinstance(entry, dict):
                continue
            key = entry.get("key")
            source = entry.get("source")
            if not key or not source:
                continue
            if source in existing:
                if existing[source].get("value") != key:
                    existing[source].set("value", key)
                    changed = True
            else:
                ET.SubElement(el, "add", {"key": source, "value": key})
                changed = True

    disabled = settings.get("disabledPackageSources")
    if isinstance(disabled, list):
        el = get_or_create(root, "disabledPackageSources")
        existing = {a.get("key") for a in el.findall("add")}
        for name in disabled:
            if not isinstance(name, str):
                continue
            if name not in existing:
                ET.SubElement(el, "add", {"key": name, "value": "true"})
                changed = True

    fallback = settings.get("fallbackPackageSources")
    if isinstance(fallback, list):
        el = get_or_create(root, "fallbackPackageSources")
        existing = {a.get("key"): a for a in el.findall("add")}
        for src in fallback:
            if not isinstance(src, dict):
                continue
            name = src.get("name")
            source = src.get("source")
            if not name or not source:
                continue
            if name in existing:
                if existing[name].get("value") != source:
                    existing[name].set("value", source)
                    changed = True
            else:
                ET.SubElement(el, "add", {"key": name, "value": source})
                changed = True

    config_el = get_or_create(root, "config")
    existing_config = {a.get("key"): a for a in config_el.findall("add")}
    config_keys = {
        "globalPackagesFolder": settings.get("globalPackagesFolder"),
        "httpProxy": settings.get("httpProxy"),
        "httpsProxy": settings.get("httpsProxy"),
        "maxHttpRequestsPerSource": settings.get("maxHttpRequestsPerSource"),
        "signatureValidationMode": settings.get("signatureValidationMode"),
    }
    for key, value in config_keys.items():
        if value is None:
            continue
        str_val = str(value)
        if key in existing_config:
            if existing_config[key].get("value") != str_val:
                existing_config[key].set("value", str_val)
                changed = True
        else:
            ET.SubElement(config_el, "add", {"key": key, "value": str_val})
            changed = True

    repo_paths = settings.get("repositoryPaths")
    if isinstance(repo_paths, list):
        el = get_or_create(root, "repositoryPaths")
        existing = {a.get("key"): a for a in el.findall("add")}
        for path in repo_paths:
            if not isinstance(path, str):
                continue
            if path not in existing:
                ET.SubElement(el, "add", {"key": path, "value": path})
                changed = True

    return changed


def check_installed():
    config_path = get_config_path()
    return shutil.which("nuget") is not None or shutil.which("dotnet") is not None or os.path.exists(config_path)


def apply_config(args, request_id):
    dry_run = args.get("dryRun", False)

    try:
        config_path = get_config_path()
        tree = read_xml(config_path)

        settings = args.get("settings", {})
        if not isinstance(settings, dict):
            settings = {}

        changed = merge_settings(tree, settings)

        if not changed:
            return {"requestId": request_id, "changed": False}

        if dry_run:
            log(f"Would update NuGet config at {config_path}")
            return {"requestId": request_id, "changed": True}

        write_xml(config_path, tree)
        log(f"Updated NuGet config: {config_path}")
        return {"requestId": request_id, "changed": True}

    except Exception as e:
        log(f"Failed to apply config: {e}")
        return {"requestId": request_id, "error": str(e)}


def main():
    input_data = sys.stdin.read()

    if not input_data:
        sys.stdout.write(json.dumps({"requestId": "unknown", "error": "No input received"}) + "\n")
        sys.stdout.flush()
        return

    try:
        request = json.loads(input_data)
    except Exception as e:
        sys.stdout.write(json.dumps({"requestId": "unknown", "error": f"Failed to parse request: {e}"}) + "\n")
        sys.stdout.flush()
        return

    request_id = request.get("requestId") or "unknown"
    command = request.get("command")
    args = request.get("args", {})

    try:
        if command == "check_installed":
            installed = check_installed()
            response = {"requestId": request_id, "installed": installed}
        elif command == "apply":
            response = apply_config(args, request_id)
        else:
            response = {"requestId": request_id, "error": f"Unknown command: {command}"}
    except Exception as fatal_err:
        response = {"requestId": request_id, "error": f"Internal Script Error: {str(fatal_err)}"}

    sys.stdout.write(json.dumps(response) + "\n")
    sys.stdout.flush()


if __name__ == "__main__":
    main()
