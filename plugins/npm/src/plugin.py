import json
import os
import shutil
import sys


def log(msg):
    sys.stderr.write(f"[npm-plugin] {msg}\n")
    sys.stderr.flush()


def get_npmrc_path():
    user_profile = os.getenv("USERPROFILE")
    if user_profile:
        return os.path.join(user_profile, ".npmrc")
    return None


def read_npmrc(file_path):
    config = {}
    if not os.path.exists(file_path):
        return config
    try:
        with open(file_path, "r", encoding="utf-8") as f:
            for line in f:
                line = line.strip()
                if not line or line.startswith(";"):
                    continue
                if "=" in line:
                    key, value = line.split("=", 1)
                    config[key.strip()] = value.strip()
    except Exception as e:
        log(f"Warning: could not read {file_path}: {e}")
    return config


def write_npmrc(file_path, config):
    os.makedirs(os.path.dirname(file_path), exist_ok=True)
    temp_path = file_path + ".tmp"
    try:
        with open(temp_path, "w", encoding="utf-8") as f:
            for key, value in config.items():
                f.write(f"{key}={value}\n")
        os.replace(temp_path, file_path)
    except Exception:
        if os.path.exists(temp_path):
            os.remove(temp_path)
        raise


def merge_config(target, source):
    changed = False
    for key, value in source.items():
        # Handle booleans and numbers by casting to string for npmrc
        if isinstance(value, bool):
            value = "true" if value else "false"
        else:
            value = str(value)

        if key not in target or target[key] != value:
            target[key] = value
            changed = True
    return changed


def check_installed(args, request_id):
    installed = (
        shutil.which("npm.cmd") is not None or shutil.which("npm") is not None
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
        npmrc_path = get_npmrc_path()
        if not npmrc_path:
            raise RuntimeError("USERPROFILE environment variable not set")

        current = read_npmrc(npmrc_path)
        changed = merge_config(current, args)

        if not changed:
            return {
                "requestId": request_id,
                "success": True,
                "changed": False,
            }

        if dry_run:
            log(f"Would update {npmrc_path} with: {json.dumps(args)}")
            return {
                "requestId": request_id,
                "success": True,
                "changed": True,
            }

        write_npmrc(npmrc_path, current)
        log(f"Updated npm config: {npmrc_path}")
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
        log(f"Failed to parse request: {e}")
        sys.exit(1)

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

    except Exception as e:
        response["error"] = f"Internal Script Error: {str(e)}"

    sys.stdout.write(json.dumps(response) + "\n")
    sys.stdout.flush()


if __name__ == "__main__":
    main()
