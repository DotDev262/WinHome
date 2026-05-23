import sys
import json
import os
import shutil

try:
    import tomllib
except ImportError:
    tomllib = None


YAZI_DIR = "yazi"
CONFIG_DIR = "config"
YAZI_TOML = "yazi.toml"
KEYMAP_TOML = "keymap.toml"
THEME_TOML = "theme.toml"

YAZI_KEYS = {
    "manager",
    "preview",
    "opener",
    "log",
    "plugin",
    "input",
    "which",
    "spotlight",
}
KEYMAP_KEYS = {"keymap", "prepend_keymap", "append_keymap"}
THEME_KEYS = {"theme", "flavor"}


def log(msg):
    sys.stderr.write(f"[yazi-plugin] {msg}\n")
    sys.stderr.flush()


def get_config_root() -> str:
    appdata = os.getenv("APPDATA")

    if not appdata:
        user_profile = os.getenv("USERPROFILE")
        if user_profile:
            appdata = os.path.join(user_profile, "AppData", "Roaming")

    if not appdata:
        raise Exception("APPDATA and USERPROFILE environment variables not found")

    return os.path.join(appdata, YAZI_DIR, CONFIG_DIR)


def read_toml(file_path: str) -> dict:
    if not os.path.exists(file_path):
        return {}

    if tomllib is None:
        raise Exception("tomllib is required to read TOML files")

    try:
        with open(file_path, "rb") as f:
            return tomllib.load(f)
    except (tomllib.TOMLDecodeError, OSError) as e:
        log(f"Warning: could not parse {file_path}: {e}")
        return {}


def write_toml(file_path: str, data: dict) -> None:
    os.makedirs(os.path.dirname(file_path), exist_ok=True)

    with open(file_path, "w", encoding="utf-8") as f:
        f.write(serialize_toml(data))


def serialize_toml(data: dict) -> str:
    lines = []

    def write_table(prefix: str, table: dict) -> None:
        simple_items = []
        nested_items = []

        for key, value in table.items():
            if value is None:
                log(f"Warning: skipping None value for key '{key}'")
                continue
            if isinstance(value, dict):
                nested_items.append((key, value))
            else:
                simple_items.append((key, value))

        if prefix:
            lines.append(f"[{prefix}]")

        for key, value in simple_items:
            lines.append(f"{key} = {serialize_value(value)}")

        if simple_items and nested_items:
            lines.append("")

        for key, value in nested_items:
            next_prefix = f"{prefix}.{key}" if prefix else key
            write_table(next_prefix, value)
            lines.append("")

        if lines and lines[-1] == "":
            lines.pop()

    def serialize_value(value) -> str:
        if value is None:
            return ""
        if isinstance(value, bool):
            return "true" if value else "false"
        if isinstance(value, (int, float)):
            return str(value)
        if isinstance(value, str):
            escaped = value.replace("\\", "\\\\").replace("\"", "\\\"")
            return f"\"{escaped}\""
        if isinstance(value, list):
            items = ", ".join(serialize_inline(v) for v in value)
            return f"[{items}]"
        if isinstance(value, dict):
            return serialize_inline(value)
        return f"\"{str(value)}\""

    def serialize_inline(value) -> str:
        if isinstance(value, dict):
            items = ", ".join(
                f"{k} = {serialize_value(v)}" for k, v in value.items()
            )
            return f"{{ {items} }}"
        return serialize_value(value)

    write_table("", data)

    content = "\n".join(lines).strip()

    return content + "\n" if content else "\n"


def merge_settings(target: dict, source: dict) -> bool:
    changed = False

    for key, value in source.items():
        if isinstance(value, dict) and isinstance(target.get(key), dict):
            if merge_settings(target[key], value):
                changed = True
        elif isinstance(value, list) and isinstance(target.get(key), list):
            for item in value:
                if item not in target[key]:
                    target[key].append(item)
                    changed = True
        else:
            if key not in target or target[key] != value:
                target[key] = value
                changed = True

    return changed


def check_installed(args: dict, request_id: str) -> dict:
    installed = shutil.which("yazi") is not None

    return {
        "requestId": request_id,
        "success": True,
        "changed": False,
        "data": {"installed": installed},
    }


def split_config(args: dict) -> dict:
    yazi = {}
    keymap = {}
    theme = {}

    for key, value in args.items():
        if key in YAZI_KEYS:
            yazi[key] = value
        elif key in KEYMAP_KEYS:
            keymap[key] = value
        elif key in THEME_KEYS:
            theme[key] = value
        else:
            log(f"Warning: unknown config key '{key}' was ignored")

    return {"yazi": yazi, "keymap": keymap, "theme": theme}


def apply_config(args: dict, context: dict, request_id: str) -> dict:
    dry_run = context.get("dryRun", False)

    try:
        config_root = get_config_root()
        config_sets = split_config(args)

        changed = False
        would_change = False

        for name, payload in config_sets.items():
            if not payload:
                continue

            if name == "yazi":
                file_path = os.path.join(config_root, YAZI_TOML)
            elif name == "keymap":
                file_path = os.path.join(config_root, KEYMAP_TOML)
            else:
                file_path = os.path.join(config_root, THEME_TOML)

            current_config = read_toml(file_path)
            local_changed = merge_settings(current_config, payload)

            if not local_changed:
                continue

            if dry_run:
                log(f"Would update {file_path} with: {json.dumps(payload)}")
                would_change = True
                continue

            write_toml(file_path, current_config)
            log(f"Updated Yazi config: {file_path}")
            changed = True

        return {
            "requestId": request_id,
            "success": True,
            "changed": changed or would_change,
            "data": {"wouldChange": would_change} if dry_run else None,
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
            response["error"] = (
                f"Unknown command: {command}"
            )

    except Exception as fatal_err:
        response["error"] = (
            f"Internal Script Error: {str(fatal_err)}"
        )

    sys.stdout.write(json.dumps(response) + "\n")
    sys.stdout.flush()


if __name__ == "__main__":
    main()