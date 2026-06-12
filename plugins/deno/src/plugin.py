import json
import os
import shutil
import sys
import tempfile
import uuid

DENO_SETTINGS = {
    "importMap",
    "compilerOptions",
    "lint",
    "fmt",
    "tasks",
    "nodeModulesDir",
    "unstable",
    "vendor",
    "permissions",
    "publish",
    "lock",
    "typeCheckOnRun",
    "watch",
}


def log(msg):
    sys.stderr.write(f"[deno-plugin] {msg}\n")
    sys.stderr.flush()


def get_deno_config_path():
    json_path = os.path.abspath("deno.json")
    jsonc_path = os.path.abspath("deno.jsonc")

    if os.path.exists(jsonc_path) and not os.path.exists(json_path):
        return jsonc_path
    return json_path


def read_deno_config(file_path):
    if not os.path.exists(file_path):
        return {}

    try:
        with open(file_path, "r", encoding="utf-8") as f:
            return json.load(f)
    except json.JSONDecodeError as e:
        log(f"Warning: could not parse {file_path} as JSON: {e}")
        return {}
    except Exception as e:
        log(f"Warning: could not read {file_path}: {e}")
        return {}


def merge_config(target, source):
    changed = False

    for key, value in source.items():
        if key in DENO_SETTINGS:
            if key not in target or target[key] != value:
                target[key] = value
                changed = True

    return changed


def backup_existing_config(file_path):
    if not os.path.exists(file_path):
        return

    backup_path = f"{file_path}.backup.{uuid.uuid4()}"
    try:
        shutil.copy2(file_path, backup_path)
        log(f"Created backup: {backup_path}")
    except Exception as e:
        log(f"Warning: could not create backup: {e}")


def write_deno_config(file_path, config):
    dir_path = os.path.dirname(file_path)
    os.makedirs(dir_path, exist_ok=True)

    backup_existing_config(file_path)

    fd, temp_path = tempfile.mkstemp(prefix="deno-", dir=dir_path)

    try:
        with os.fdopen(fd, "w", encoding="utf-8", newline="\n") as f:
            json.dump(config, f, indent=2)
            f.write("\n")

        os.replace(temp_path, file_path)

    except Exception:
        try:
            os.unlink(temp_path)
        except OSError:
            pass
        raise


def check_installed():
    return (
        shutil.which("deno.cmd") is not None
        or shutil.which("deno.exe") is not None
        or shutil.which("deno") is not None
    )


def apply_config(args, request_id):
    dry_run = args.get("dryRun", False)
    settings = args.get("settings", {})

    if not isinstance(settings, dict):
        return {
            "requestId": request_id,
            "changed": False,
            "error": "settings must be an object",
        }

    try:
        config_path = get_deno_config_path()
        current_config = read_deno_config(config_path)

        changed = merge_config(current_config, settings)

        if not changed:
            return {"requestId": request_id, "changed": False}

        if dry_run:
            log(f"Would update {config_path} with: {json.dumps(settings)}")
            return {
                "requestId": request_id,
                "changed": True,
            }

        write_deno_config(config_path, current_config)
        log(f"Updated deno config: {config_path}")

        return {"requestId": request_id, "changed": True}

    except Exception as e:
        log(f"Failed to apply config: {e}")
        return {"requestId": request_id, "changed": False, "error": str(e)}


def main():
    input_data = sys.stdin.read()

    if not input_data.strip():
        response = {
            "requestId": "unknown",
            "error": "No input provided on stdin",
        }
        sys.stdout.write(json.dumps(response) + "\n")
        sys.stdout.flush()
        return

    try:
        request = json.loads(input_data)
    except Exception as e:
        response = {
            "requestId": "unknown",
            "error": f"Failed to parse JSON request: {str(e)}",
        }
        sys.stdout.write(json.dumps(response) + "\n")
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
