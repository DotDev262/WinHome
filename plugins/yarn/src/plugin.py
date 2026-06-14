import json
import os
import shutil
import sys
import uuid

def log(msg):
    sys.stderr.write(f"[yarn-plugin] {msg}\n")
    sys.stderr.flush()

def _safe_bool(value):
    return bool(value)

def _yaml_quote_string(value: str) -> str:
    # Minimal YAML quoting: quote if it contains special chars or starts with certain tokens.
    s = value
    if s == "" or any(ch in s for ch in [":", "#", "{", "}", "[", "]", "&", "*", "!", "|", ">", "?", "-", "@", ",", "\n", "\r", "\t"]) or s.startswith(("{", "[", "*", "&", "!", "|", ">", "-", "?", "@")):
        escaped = s.replace("\\", "\\\\").replace('"', '\\"')
        return f'"{escaped}"'
    return s

def _to_yaml_value(value):
    if isinstance(value, bool):
        return "true" if value else "false"
    if isinstance(value, int):
        return str(value)
    if isinstance(value, dict):
        # Emit inline mapping
        parts = []
        for k, v in value.items():
            parts.append(f"{k}: {_to_yaml_value(v)}")
        return "{" + ", ".join(parts) + "}"
    return _yaml_quote_string(str(value))

def _yarnrcyml_dump(settings: dict) -> str:
    # Deterministic ordering for stable writes.
    lines = []
    for key in sorted(settings.keys()):
        val = settings[key]
        # Yarn Berry supports nested objects
        if isinstance(val, dict):
            lines.append(f"{key}:")
            for arch_key in sorted(val.keys()):
                lines.append(f"  {arch_key}: {_to_yaml_value(val[arch_key])}")
        else:
            lines.append(f"{key}: {_to_yaml_value(val)}")
    return "\n".join(lines) + "\n"

def _yarnrc_dump_classic(settings: dict) -> str:
    # Yarn classic .yarnrc uses `key value` per line.
    def dump_value(v):
        if isinstance(v, bool):
            return "true" if v else "false"
        if isinstance(v, int):
            return str(v)
        if isinstance(v, dict):
            return json.dumps(v, ensure_ascii=False, separators=(",", ":"))
        return str(v)

    lines = []
    for key in sorted(settings.keys()):
        lines.append(f"{key} {dump_value(settings[key])}")
    return "\n".join(lines) + "\n"

def _parse_existing_kv_lines(lines: str) -> dict:
    result = {}
    for raw in lines.splitlines():
        s = raw.strip()
        if not s or s.startswith("#") or s.startswith(";"):
            continue
        if "=" in s:
            k, v = s.split("=", 1)
            result[k.strip()] = v.strip()
            continue
        parts = s.split(None, 1)
        if len(parts) == 2:
            k, v = parts
            result[k.strip()] = v.strip()
    return result

def _get_user_home():
    return os.path.expanduser("~")

def get_yarnrc_paths():
    home = _get_user_home()
    return {
        "berry": os.path.join(home, ".yarnrc.yml"),
        "classic": os.path.join(home, ".yarnrc"),
    }

def log_error(error_msg):
    log(error_msg)

def _atomic_write(path: str, content: str):
    dir_path = os.path.dirname(path) or "."
    os.makedirs(dir_path, exist_ok=True)
    uid = uuid.uuid4().hex

    backup_path = f"{path}.backup.{uid}"
    if os.path.exists(path):
        shutil.copy2(path, backup_path)

    temp_path = f"{path}.tmp.{uid}"
    try:
        with open(temp_path, "w", encoding="utf-8", newline="\n") as f:
            f.write(content)
        os.replace(temp_path, path)
    except Exception:
        try:
            if os.path.exists(temp_path):
                os.remove(temp_path)
        except OSError:
            pass
        raise

def check_installed(args: dict) -> bool:
    paths = get_yarnrc_paths()
    found_yarn = (
        shutil.which("yarn.cmd") is not None
        or shutil.which("yarn.exe") is not None
        or shutil.which("yarn") is not None
    )

    cfg_exists = os.path.exists(paths["berry"]) or os.path.exists(paths["classic"])
    return bool(found_yarn or cfg_exists)


YARN_SETTING_KEYS = {
    "nodeLinker",
    "packageManager",
    "npmRegistryServer",
    "npmAuthToken",
    "caFilePath",
    "enableImmutableInstalls",
    "enableTelemetry",
    "yarnPath",
    "supportedArchitectures",
    "pnpEnable",
    "pnpFallbackMode",
    "compressionLevel",
}

def _response(request_id: str, changed: bool, error: str = None) -> dict:
    # JSON-RPC response format for this plugin:
    # - On success: {requestId, changed}
    # - On failure: {requestId, changed, error}
    resp = {
        "requestId": request_id,
        "changed": changed,
    }
    if error is not None:
        resp["error"] = error
    return resp


def _validate_and_normalize_settings(settings_raw: object):
    if not isinstance(settings_raw, dict):
        raise TypeError("settings must be an object")

    settings = {}
    for k, v in settings_raw.items():
        if k not in YARN_SETTING_KEYS:
            continue

        if k in {"enableImmutableInstalls", "enableTelemetry", "pnpEnable"}:
            settings[k] = _safe_bool(v)
        elif k == "compressionLevel":
            settings[k] = int(v)
        elif k == "supportedArchitectures":
            if isinstance(v, dict):
                settings[k] = v
            else:
                raise TypeError("supportedArchitectures must be an object")
        else:
            settings[k] = str(v)

    return settings

def _apply_berry(berry_path: str, settings: dict, dry_run: bool, request_id: str) -> dict:
    existing = {}
    if os.path.exists(berry_path):
        try:
            with open(berry_path, "r", encoding="utf-8") as f:
                for raw in f.read().splitlines():
                    s = raw.strip()
                    if not s or s.startswith("#"):
                        continue
                    if ":" in s:
                        k, _ = s.split(":", 1)
                        existing[k.strip()] = True
        except Exception as e:
            log(f"Warning: could not read existing {berry_path}: {e}")

    merged = dict(existing)
    for k, v in settings.items():
        merged[k] = v

    content = _yarnrcyml_dump(settings=merged)

    if dry_run:
        return {
            "requestId": request_id,
            "changed": True,
        }

    _atomic_write(berry_path, content)
    return {
        "requestId": request_id,
        "changed": True,
    }


def _apply_classic(classic_path: str, settings: dict, dry_run: bool, request_id: str) -> dict:
    existing = {}
    if os.path.exists(classic_path):
        try:
            with open(classic_path, "r", encoding="utf-8") as f:
                existing = _parse_existing_kv_lines(f.read())
        except Exception as e:
            log(f"Warning: could not read existing {classic_path}: {e}")

    merged = dict(existing)
    for k, v in settings.items():
        merged[k] = v

    content = _yarnrc_dump_classic(merged)

    if dry_run:
        return {
            "requestId": request_id,
            "changed": True,
        }

    _atomic_write(classic_path, content)
    return {
        "requestId": request_id,
        "changed": True,
    }


def apply_config(args: dict, context: dict, request_id: str) -> dict:
    dry_run = bool(context.get("dryRun", False))

    try:
        settings_raw = args.get("settings", {})
        if not isinstance(settings_raw, dict):
            raise TypeError("settings must be an object")

        settings = _validate_and_normalize_settings(settings_raw)

        if not settings:
            return {
                "requestId": request_id,
                "changed": False,
            }


        paths = get_yarnrc_paths()
        berry_exists = os.path.exists(paths["berry"])
        classic_exists = os.path.exists(paths["classic"])

        if berry_exists or (not classic_exists):
            return _apply_berry(paths["berry"], settings, dry_run=dry_run, request_id=request_id)

        return _apply_classic(paths["classic"], settings, dry_run=dry_run, request_id=request_id)

    except Exception as e:
        log_error(f"Failed to apply config: {e}")
        return {
            "requestId": request_id,
            "changed": False,
            "error": str(e),
        }


def main():
    input_data = sys.stdin.read()

    if not input_data:
        response = {
            "requestId": "unknown",
            "changed": False,
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
            "changed": False,
            "error": f"Failed to parse JSON request: {str(e)}",
        }

        sys.stdout.write(json.dumps(response) + "\n")
        sys.stdout.flush()
        return

    request_id = request.get("requestId") or "unknown"

    command = request.get("command")
    args = request.get("args", {})
    context = request.get("context", {})

    try:
        if command == "check_installed":
            installed = check_installed(args)
            response = {
                "requestId": request_id,
                "installed": installed,
            }

        elif command == "apply":
            response = apply_config(args, context, request_id)
        else:
            response = {
                "requestId": request_id,
                "changed": False,
                "error": f"Unknown command: {command}",
            }

    except Exception as e:
        response = {
            "requestId": request_id,
            "changed": False,
            "error": f"Internal Script Error: {str(e)}",
        }


    sys.stdout.write(json.dumps(response) + "\n")
    sys.stdout.flush()

if __name__ == "__main__":
    main()

