import copy
import json
import os
import shutil
import sys
import tempfile
import uuid
from pathlib import Path


PLUGIN_NAME = "zed"
SETTINGS_FILE = "settings.json"
NON_SETTING_ARG_KEYS = {
    "configPath",
    "config_path",
}
INT_SETTING_KEYS = {
    "buffer_font_size",
    "font_size",
    "tab_size",
}
BOOL_SETTING_KEYS = {
    "vim_mode",
    "relative_line_numbers",
    "copilot",
}


def log(message: str) -> None:
    sys.stderr.write(f"[{PLUGIN_NAME}-plugin] {message}\n")
    sys.stderr.flush()


def response(request_id: str, success: bool, changed: bool, error=None, data=None) -> dict:
    result = {
        "requestId": request_id,
        "success": success,
        "changed": changed,
    }

    if error is not None:
        result["error"] = error

    if data is not None:
        result["data"] = data

    return result


def strip_jsonc_comments(text: str) -> str:
    output = []
    in_string = False
    escaped = False
    index = 0

    while index < len(text):
        char = text[index]
        next_char = text[index + 1] if index + 1 < len(text) else ""

        if in_string:
            output.append(char)
            if escaped:
                escaped = False
            elif char == "\\":
                escaped = True
            elif char == '"':
                in_string = False
            index += 1
            continue

        if char == '"':
            in_string = True
            output.append(char)
            index += 1
            continue

        if char == "/" and next_char == "/":
            while index < len(text) and text[index] not in "\r\n":
                index += 1
            continue

        if char == "/" and next_char == "*":
            index += 2
            while index < len(text):
                if text[index] == "*" and index + 1 < len(text) and text[index + 1] == "/":
                    index += 2
                    break
                index += 1
            continue

        output.append(char)
        index += 1

    return "".join(output)


def appdata_dir() -> str:
    appdata = os.getenv("APPDATA")
    if appdata:
        return appdata

    userprofile = os.getenv("USERPROFILE")
    if userprofile:
        return os.path.join(userprofile, "AppData", "Roaming")

    return str(Path.home() / "AppData" / "Roaming")


def expand_path(path: str) -> str:
    return os.path.abspath(os.path.expandvars(os.path.expanduser(path)))


def get_config_path(args: dict, context: dict) -> str:
    explicit_path = (
        args.get("configPath")
        or args.get("config_path")
        or context.get("configPath")
        or context.get("config_path")
    )

    if explicit_path:
        return expand_path(str(explicit_path))

    return os.path.join(appdata_dir(), "Zed", SETTINGS_FILE)


def read_jsonc(file_path: str) -> dict:
    if not os.path.exists(file_path):
        return {}

    try:
        with open(file_path, "r", encoding="utf-8") as config_file:
            text = config_file.read()

        if not text.strip():
            return {}

        parsed = json.loads(strip_jsonc_comments(text))
        if isinstance(parsed, dict):
            return parsed

        log(f"Warning: expected object in {file_path}, got {type(parsed).__name__}")
        return {}
    except Exception as exc:
        log(f"Warning: could not parse {file_path}: {exc}")
        backup_corrupt_file(file_path)
        return {}


def backup_corrupt_file(file_path: str) -> None:
    backup_path = f"{file_path}.corrupt-{uuid.uuid4()}.bak"
    try:
        shutil.copy2(file_path, backup_path)
        log(f"Backed up unreadable config to: {backup_path}")
    except Exception as exc:
        log(f"Warning: could not back up unreadable config: {exc}")


def write_json(file_path: str, data: dict) -> None:
    os.makedirs(os.path.dirname(file_path), exist_ok=True)
    temp_fd, temp_path = tempfile.mkstemp(
        prefix="zed-",
        suffix=".tmp",
        dir=os.path.dirname(file_path),
    )

    try:
        with os.fdopen(temp_fd, "w", encoding="utf-8") as config_file:
            json.dump(data, config_file, indent=2)
            config_file.write("\n")

        os.replace(temp_path, file_path)
    except Exception:
        if os.path.exists(temp_path):
            os.remove(temp_path)
        raise


def desired_config_from_args(args: dict) -> dict:
    if not isinstance(args, dict):
        return {}

    if "settings" in args and isinstance(args["settings"], dict):
        return normalize_config(copy.deepcopy(args["settings"]))

    desired = {
        key: copy.deepcopy(value)
        for key, value in args.items()
        if key not in NON_SETTING_ARG_KEYS
    }
    return normalize_config(desired)


def normalize_scalar(key: str, value):
    if not isinstance(value, str):
        return value

    stripped = value.strip()
    lowered = stripped.lower()

    if key in BOOL_SETTING_KEYS or lowered in {"true", "false"}:
        if lowered == "true":
            return True
        if lowered == "false":
            return False

    if key in INT_SETTING_KEYS:
        try:
            return int(stripped)
        except ValueError:
            return value

    return value


def normalize_config(config: dict) -> dict:
    normalized = {}

    for key, value in config.items():
        if isinstance(value, dict):
            normalized[key] = normalize_config(value)
        elif isinstance(value, list):
            normalized[key] = [
                normalize_config(item) if isinstance(item, dict) else item
                for item in value
            ]
        else:
            normalized[key] = normalize_scalar(key, value)

    return normalized


def deep_merge(target: dict, source: dict) -> bool:
    changed = False

    for key, value in source.items():
        if isinstance(value, dict) and isinstance(target.get(key), dict):
            changed = deep_merge(target[key], value) or changed
            continue

        if target.get(key) != value:
            target[key] = copy.deepcopy(value)
            changed = True

    return changed


def check_installed(args: dict, request_id: str) -> dict:
    installed = shutil.which("zed.exe") is not None or shutil.which("zed") is not None
    return response(
        request_id,
        success=True,
        changed=False,
        data=installed,
    )


def apply_config(args: dict, context: dict, request_id: str) -> dict:
    dry_run = bool(context.get("dryRun", False))

    try:
        config_path = get_config_path(args, context)
        desired = desired_config_from_args(args)
        current = read_jsonc(config_path)
        next_config = copy.deepcopy(current)
        changed = deep_merge(next_config, desired)

        if not changed:
            return response(request_id, success=True, changed=False)

        if dry_run:
            log(
                "Would update "
                f"{config_path} with keys: {', '.join(sorted(desired.keys()))}"
            )
            return response(request_id, success=True, changed=True)

        write_json(config_path, next_config)
        log(f"Updated Zed config: {config_path}")

        return response(request_id, success=True, changed=True)

    except Exception as exc:
        log(f"Failed to apply config: {exc}")
        return response(request_id, success=False, changed=False, error=str(exc))


def process_request(request: dict) -> dict:
    request_id = request.get("requestId", "unknown")
    command = request.get("command")
    args = request.get("args", request.get("config", {}))
    context = request.get("context", {})

    if "dryRun" in request and "dryRun" not in context:
        context = dict(context)
        context["dryRun"] = request.get("dryRun")

    if command == "check_installed":
        return check_installed(args, request_id)

    if command == "apply":
        return apply_config(args, context, request_id)

    return response(
        request_id,
        success=False,
        changed=False,
        error=f"Unknown command: {command}",
    )


def main() -> None:
    input_data = sys.stdin.read()

    if not input_data:
        result = response("unknown", success=False, changed=False, error="Empty input")
        sys.stdout.write(json.dumps(result) + "\n")
        sys.stdout.flush()
        return

    try:
        request = json.loads(input_data)
        result = process_request(request)
    except Exception as exc:
        log(f"Internal Script Error: {exc}")
        result = response("unknown", success=False, changed=False, error=str(exc))

    sys.stdout.write(json.dumps(result) + "\n")
    sys.stdout.flush()


if __name__ == "__main__":
    main()
