import json
import os
import shutil
import sys

try:
    import tomllib
except ImportError:
    tomllib = None


def log(msg):
    sys.stderr.write(f"[starship-plugin] {msg}\n")
    sys.stderr.flush()


def get_config_path():
    userprofile = os.getenv("USERPROFILE")
    if not userprofile:
        raise Exception("USERPROFILE environment variable not found")

    config_dir = os.path.join(userprofile, ".config")
    os.makedirs(config_dir, exist_ok=True)

    return os.path.join(config_dir, "starship.toml")


def read_toml(file_path: str) -> dict:
    if not os.path.exists(file_path):
        return {}

    if tomllib:
        try:
            with open(file_path, "rb") as f:
                return tomllib.load(f)
        except Exception as e:
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
    else:
        return json.dumps(v, ensure_ascii=False)


def dump_toml(data: dict) -> str:
    lines = []

    # Primitives first
    for k, v in data.items():
        if not isinstance(v, dict):
            lines.append(f"{k} = {dump_value(v)}")

    # Tables after
    for k, v in data.items():
        if isinstance(v, dict):
            lines.append("")
            lines.append(f"[{k}]")
            for sub_k, sub_v in v.items():
                lines.append(f"{sub_k} = {dump_value(sub_v)}")

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
        else:
            if key not in target or target[key] != value:
                target[key] = value
                changed = True
    return changed


def check_installed(args: dict, request_id: str) -> dict:
    installed = (
        shutil.which("starship.exe") is not None
        or shutil.which("starship") is not None
    )
    return {
        "requestId": request_id,
        "success": True,
        "changed": False,
        "data": {"installed": installed},
    }


def apply_config(args: dict, context: dict, request_id: str) -> dict:
    dry_run = context.get("dryRun", False)
    settings = args

    try:
        config_path = get_config_path()
        current_config = read_toml(config_path)

        changed = merge_settings(current_config, settings)

        if not changed:
            return {
                "requestId": request_id,
                "success": True,
                "changed": False,
            }

        if dry_run:
            log(f"Would update {config_path} with new settings")
            return {
                "requestId": request_id,
                "success": True,
                "changed": True,
            }

        write_toml(config_path, current_config)
        log(f"Updated Starship config: {config_path}")

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
