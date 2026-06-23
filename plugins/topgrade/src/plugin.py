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
    return installed


def apply_config(args, request_id):
    dry_run = args.get("dryRun", False)
    settings = args.get("settings", {})

    if not isinstance(settings, dict):
        raise ValueError("settings must be an object")

    config_path = get_topgrade_config_path()
    if not config_path:
        raise ValueError("APPDATA environment variable not set")

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

        if not changed or dry_run:
            if dry_run:
                log(f"Would update {config_path}")
            return {"changed": changed}

        import tempfile

        fd, temp_path = tempfile.mkstemp(dir=os.path.dirname(config_path), text=True)
        try:
            with os.fdopen(fd, "w", encoding="utf-8") as f:
                f.write(new_content)
            os.replace(temp_path, config_path)
        except Exception as e:
            os.remove(temp_path)
            raise e

        log(f"Updated topgrade config: {config_path}")
        return {"changed": True}

    except Exception as e:
        log(f"Failed to apply config: {e}")
        raise e


def main():
    input_data = sys.stdin.read()

    if not input_data:
        response = {"requestId": "unknown", "error": "No input received"}
        sys.stdout.write(json.dumps(response) + "\n")
        sys.stdout.flush()
        return

    try:
        request = json.loads(input_data)
    except Exception as e:
        response = {"requestId": "unknown", "error": f"Failed to parse JSON request: {str(e)}"}
        sys.stdout.write(json.dumps(response) + "\n")
        sys.stdout.flush()
        return

    request_id = request.get("requestId")
    if request_id is None:
        request_id = "unknown"

    command = request.get("command")
    args = request.get("args", {})

    response = {"requestId": request_id}

    try:
        if command == "check_installed":
            installed = check_installed(args, request_id)
            response["installed"] = installed
        elif command == "apply":
            res = apply_config(args, request_id)
            response.update(res)
        else:
            response["error"] = f"Unknown command: {command}"
    except Exception as fatal_err:
        response["error"] = f"Internal error: {str(fatal_err)}"

    sys.stdout.write(json.dumps(response) + "\n")
    sys.stdout.flush()


if __name__ == "__main__":
    main()
