# /// script
# requires-python = ">=3.11"
# dependencies = [
#     "tomlkit",
# ]
# ///

import json
import os
import shutil
import sys

import tomlkit


def log(msg):
    sys.stderr.write(f"[topgrade-plugin] {msg}\n")
    sys.stderr.flush()


def get_topgrade_config_path():
    appdata = os.environ.get("APPDATA")
    if not appdata:
        return None

    main_path = os.path.join(appdata, "topgrade", "topgrade.toml")
    fallback_path = os.path.join(appdata, "topgrade.toml")

    if os.path.exists(main_path):
        return main_path
    if os.path.exists(fallback_path):
        return fallback_path

    return main_path


def merge_dict(target, source):
    for k, v in source.items():
        if isinstance(v, dict):
            if k not in target:
                target[k] = tomlkit.table()
            if isinstance(target[k], dict):
                merge_dict(target[k], v)
            else:
                target[k] = v
        else:
            target[k] = v


def check_installed(args, request_id):
    installed = shutil.which("topgrade") is not None
    return {
        "requestId": request_id,
        "success": True,
        "changed": False,
        "data": installed,
    }


def apply_config(args, request_id):
    dry_run = args.get("dryRun", False)
    settings = args.get("settings", {})

    if not isinstance(settings, dict):
        return {
            "requestId": request_id,
            "success": False,
            "changed": False,
            "error": "settings must be an object",
            "data": None,
        }

    config_path = get_topgrade_config_path()
    if not config_path:
        return {
            "requestId": request_id,
            "success": False,
            "changed": False,
            "error": "APPDATA environment variable not set",
            "data": None,
        }

    try:
        if os.path.exists(config_path):
            with open(config_path, "r", encoding="utf-8") as f:
                doc = tomlkit.load(f)
        else:
            doc = tomlkit.document()
            os.makedirs(os.path.dirname(config_path), exist_ok=True)

        orig_content = doc.as_string()
        merge_dict(doc, settings)
        new_content = doc.as_string()
        changed = orig_content != new_content

        if not changed:
            return {
                "requestId": request_id,
                "success": True,
                "changed": False,
                "data": None,
            }

        if dry_run:
            log(f"Would update {config_path}")
            return {
                "requestId": request_id,
                "success": True,
                "changed": True,
                "data": {"path": config_path, "settings": settings},
            }

        with open(config_path, "w", encoding="utf-8") as f:
            f.write(new_content)

        log(f"Updated topgrade config: {config_path}")
        return {
            "requestId": request_id,
            "success": True,
            "changed": True,
            "data": {"path": config_path},
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
            "error": "No input received",
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

    request_id = request.get("requestId") or "unknown"
    command = request.get("command")
    args = request.get("args", {})

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
            response = apply_config(args, request_id)
        else:
            response["error"] = f"Unknown command: {command}"
    except Exception as fatal_err:
        response["error"] = f"Internal error: {str(fatal_err)}"

    sys.stdout.write(json.dumps(response) + "\n")
    sys.stdout.flush()


if __name__ == "__main__":
    main()
