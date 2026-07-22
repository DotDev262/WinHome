import json
import os
import shutil
import sys
import tempfile
import uuid

CONFIG_FILE = ".npmrc"


PNPM_SETTING_MAP = {
    "storeDir": "store-dir",
    "globalDir": "global-dir",
    "globalBinDir": "global-bin-dir",
    "nodeVersion": "node-version",
    "packageManager": "package-manager",
    "autoInstallPeers": "auto-install-peers",
    "strictPeerDependencies": "strict-peer-dependencies",
    "shamefullyHoist": "shamefully-hoist",
}


def log(msg):
    sys.stderr.write(f"[pnpm-plugin] {msg}\n")
    sys.stderr.flush()


def get_npmrc_path():
    user_profile = os.getenv("USERPROFILE") or os.path.expanduser("~")
    return os.path.join(user_profile, CONFIG_FILE)


def read_npmrc(file_path: str) -> dict:
    config = {}

    if not os.path.exists(file_path):
        return config

    try:
        with open(file_path, "r", encoding="utf-8") as f:
            for line in f:
                stripped = line.strip()

                if not stripped or stripped.startswith("#") or stripped.startswith(";"):
                    continue

                if "=" in stripped:
                    key, value = stripped.split("=", 1)
                    config[key.strip()] = value.strip()

    except Exception as e:
        log(f"Warning: could not read {file_path}: {e}")

    return config


def normalize_value(value):
    if isinstance(value, bool):
        return "true" if value else "false"

    if value is None:
        return ""

    return str(value)


def normalize_settings(settings: dict) -> dict:
    normalized = {}

    for key, value in settings.items():
        npmrc_key = PNPM_SETTING_MAP.get(key, key)
        normalized[npmrc_key] = normalize_value(value)

    return normalized


def merge_config(target: dict, source: dict) -> bool:
    changed = False

    normalized_source = normalize_settings(source)

    for key, value in normalized_source.items():
        if key not in target or target[key] != value:
            target[key] = value
            changed = True

    return changed


def backup_existing_config(file_path: str) -> None:
    if not os.path.exists(file_path):
        return

    backup_path = f"{file_path}.backup.{uuid.uuid4()}"

    shutil.copy2(file_path, backup_path)
    log(f"Created backup: {backup_path}")


def build_npmrc_content(config: dict) -> str:
    lines = []

    for key, value in config.items():
        lines.append(f"{key}={value}")

    return "\n".join(lines) + "\n"


def write_npmrc(file_path: str, config: dict) -> None:
    dir_path = os.path.dirname(file_path)
    os.makedirs(dir_path, exist_ok=True)

    backup_existing_config(file_path)

    fd, temp_path = tempfile.mkstemp(prefix="pnpm-", dir=dir_path)

    try:
        with os.fdopen(fd, "w", encoding="utf-8", newline="\n") as f:
            f.write(build_npmrc_content(config))

        os.replace(temp_path, file_path)

    except Exception:
        try:
            os.unlink(temp_path)
        except OSError:
            pass

        raise


def check_installed(args: dict, request_id: str) -> dict:
    installed = (
        shutil.which("pnpm.cmd") is not None or shutil.which("pnpm.exe") is not None or shutil.which("pnpm") is not None
    )

    return {
        "requestId": request_id,
        "success": True,
        "changed": False,
        "data": installed,
    }


def apply_config(args: dict, context: dict, request_id: str) -> dict:
    dry_run = context.get("dryRun", False)
    settings = args.get("settings", {})

    if not isinstance(settings, dict):
        return {
            "requestId": request_id,
            "success": False,
            "changed": False,
            "error": "settings must be an object",
            "data": None,
        }

    try:
        npmrc_path = get_npmrc_path()
        current_config = read_npmrc(npmrc_path)

        changed = merge_config(current_config, settings)

        if not changed:
            return {
                "requestId": request_id,
                "success": True,
                "changed": False,
                "data": None,
            }

        if dry_run:
            log(f"Would update {npmrc_path} with: {json.dumps(settings)}")

            return {
                "requestId": request_id,
                "success": True,
                "changed": True,
                "data": {
                    "path": npmrc_path,
                    "settings": settings,
                },
            }

        write_npmrc(npmrc_path, current_config)

        log(f"Updated pnpm config: {npmrc_path}")

        return {
            "requestId": request_id,
            "success": True,
            "changed": True,
            "data": {
                "path": npmrc_path,
            },
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
            "error": "No input provided on stdin",
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

    request_id = request.get("requestId", "unknown")
    command = request.get("command")
    args = request.get("args", {})
    context = request.get("context", {})

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
            response = apply_config(args, context, request_id)
        else:
            response["error"] = f"Unknown command: {command}"

    except Exception as fatal_err:
        response["error"] = f"Internal Script Error: {str(fatal_err)}"

    sys.stdout.write(json.dumps(response) + "\n")
    sys.stdout.flush()


if __name__ == "__main__":
    main()
