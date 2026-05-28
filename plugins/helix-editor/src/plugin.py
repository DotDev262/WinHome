import json
import os
import shutil
import sys

try:
    import tomllib
except ImportError:
    tomllib = None


def log(msg):
    sys.stderr.write(f"[helix-plugin] {msg}\n")
    sys.stderr.flush()


def get_helix_dir():
    appdata = os.getenv("APPDATA")
    if not appdata:
        # Fallback if APPDATA isn't set, though it should be on Windows
        userprofile = os.getenv("USERPROFILE")
        if userprofile:
            appdata = os.path.join(userprofile, "AppData", "Roaming")
        else:
            raise Exception("APPDATA environment variable not found")

    helix_dir = os.path.join(appdata, "helix")

    return helix_dir


def read_toml(file_path: str) -> dict:
    if not os.path.exists(file_path):
        return {}

    if tomllib:
        try:
            with open(file_path, "rb") as f:
                return tomllib.load(f)
        except (tomllib.TOMLDecodeError, OSError) as e:
            log(f"Warning: could not parse {file_path} using tomllib: {e}")
            return {}
    else:
        log(
            "Warning: tomllib not available (requires Python 3.11+). Starting with empty config."
        )
        return {}


def dump_value(v):
    if isinstance(v, bool):
        return "true" if v else "false"
    elif isinstance(v, str):
        return json.dumps(v, ensure_ascii=False)
    elif isinstance(v, (int, float)):
        return str(v)
    elif isinstance(v, list):
        items = ", ".join(dump_value(item) for item in v)
        return f"[{items}]"
    else:
        return json.dumps(v, ensure_ascii=False)


def _dump_dict_recursive(d: dict, prefix: str, lines: list):
    # Primitives first
    for k, v in d.items():
        if not isinstance(v, dict) and not (
            isinstance(v, list) and len(v) > 0 and isinstance(v[0], dict)
        ):
            lines.append(f"{k} = {dump_value(v)}")

    # Array of Tables
    for k, v in d.items():
        if isinstance(v, list) and len(v) > 0 and isinstance(v[0], dict):
            for item in v:
                lines.append("")
                arr_path = f"{prefix}.{k}" if prefix else k
                lines.append(f"[[{arr_path}]]")
                _dump_dict_recursive(item, arr_path, lines)

    # Tables after
    for k, v in d.items():
        if isinstance(v, dict):
            lines.append("")
            table_path = f"{prefix}.{k}" if prefix else k
            lines.append(f"[{table_path}]")
            _dump_dict_recursive(v, table_path, lines)


def dump_toml(data: dict) -> str:
    lines = []
    _dump_dict_recursive(data, "", lines)
    return "\n".join(lines).strip() + "\n"


def write_toml(file_path: str, data: dict) -> None:
    os.makedirs(os.path.dirname(file_path), exist_ok=True)
    toml_str = dump_toml(data)
    with open(file_path, "w", encoding="utf-8") as f:
        f.write(toml_str)


def merge_settings(target: dict, source: dict) -> bool:
    changed = False
    for key, value in source.items():
        if isinstance(value, dict):
            if key not in target or not isinstance(target.get(key), dict):
                target[key] = {}
                changed = True

            # Recursive merge for deep dictionaries
            if merge_settings(target[key], value):
                changed = True
        elif (
            isinstance(value, list)
            and len(value) > 0
            and isinstance(value[0], dict)
        ):
            # Array of tables merge (e.g., [[language]])
            if key not in target:
                target[key] = []

            # Simple replacement or append logic for arrays of tables based on "name"
            for item in value:
                if "name" in item:
                    existing_item = next(
                        (
                            i
                            for i in target[key]
                            if isinstance(i, dict)
                            and i.get("name") == item["name"]
                        ),
                        None,
                    )
                    if existing_item:
                        if merge_settings(existing_item, item):
                            changed = True
                    else:
                        target[key].append(item)
                        changed = True
                else:
                    if item not in target[key]:
                        target[key].append(item)
                        changed = True
        else:
            if key not in target or target[key] != value:
                target[key] = value
                changed = True
    return changed


def check_installed(args: dict, request_id: str) -> dict:
    installed = (
        shutil.which("hx.exe") is not None or shutil.which("hx") is not None
    )
    return {
        "requestId": request_id,
        "success": True,
        "changed": False,
        "data": {"installed": installed},
    }


def apply_config(args: dict, context: dict, request_id: str) -> dict:
    dry_run = context.get("dryRun", False)

    if "config" in args or "languages" in args:
        config_settings = args.get("config", {})
        language_settings = args.get("languages", {})
    else:
        config_settings = {}
        language_settings = {}
        for k, v in args.items():
            if k in ("language", "language-server"):
                language_settings[k] = v
            else:
                config_settings[k] = v

    try:
        helix_dir = get_helix_dir()
        config_path = os.path.join(helix_dir, "config.toml")
        languages_path = os.path.join(helix_dir, "languages.toml")

        changed = False

        # Handle config.toml
        if config_settings:
            current_config = read_toml(config_path)
            if merge_settings(current_config, config_settings):
                changed = True
                if not dry_run:
                    write_toml(config_path, current_config)
                    log(f"Updated config: {config_path}")

        # Handle languages.toml
        if language_settings:
            current_languages = read_toml(languages_path)
            if merge_settings(current_languages, language_settings):
                changed = True
                if not dry_run:
                    write_toml(languages_path, current_languages)
                    log(f"Updated languages: {languages_path}")

        if not changed:
            return {
                "requestId": request_id,
                "success": True,
                "changed": False,
            }

        if dry_run:
            log("Would update Helix config with new settings")
            return {
                "requestId": request_id,
                "success": True,
                "changed": True,
            }

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
    except Exception as fatal_err:
        response["error"] = f"Internal Script Error: {str(fatal_err)}"

    sys.stdout.write(json.dumps(response) + "\n")
    sys.stdout.flush()


if __name__ == "__main__":
    main()
