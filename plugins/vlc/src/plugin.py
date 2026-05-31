import datetime
import json
import os
import re
import shutil
import sys
import tempfile
import uuid


def log(msg: str) -> None:
    sys.stderr.write(f"[vlc-plugin] {msg}\n")
    sys.stderr.flush()


def get_config_path() -> str:
    appdata = os.getenv("APPDATA")
    if not appdata:
        raise RuntimeError("APPDATA environment variable not found")
    return os.path.join(appdata, "vlc", "vlcrc")


def read_text(file_path: str) -> str:
    if not os.path.exists(file_path):
        return ""
    try:
        with open(file_path, "r", encoding="utf-8") as f:
            return f.read()
    except (OSError, UnicodeDecodeError) as exc:
        timestamp = datetime.datetime.now(datetime.timezone.utc).strftime(
            "%Y%m%d%H%M%S"
        )
        suffix = uuid.uuid4().hex[:8]
        backup_path = f"{file_path}.corrupted.{timestamp}.{suffix}"
        log(
            f"Config corrupted. Backing up to {backup_path} and starting fresh. "
            f"Error: {exc}"
        )
        try:
            shutil.move(file_path, backup_path)
        except OSError as backup_exc:
            log(f"Failed to backup corrupted config: {backup_exc}")
        return ""


def write_text(file_path: str, data: str) -> None:
    dir_path = os.path.dirname(file_path)
    if dir_path:
        os.makedirs(dir_path, exist_ok=True)

    fd, temp_path = tempfile.mkstemp(
        prefix="vlcrc.", suffix=".tmp", dir=dir_path or None, text=True
    )
    try:
        with os.fdopen(fd, "w", encoding="utf-8", newline="\n") as handle:
            handle.write(data)
        os.replace(temp_path, file_path)
    except Exception:
        try:
            os.remove(temp_path)
        except OSError:
            pass
        raise


def parse_ini(text: str) -> tuple:
    blocks = [{"name": None, "lines": []}]
    current_block = blocks[0]
    has_trailing_newline = text.endswith("\n")
    is_crlf = "\r\n" in text

    for line in text.splitlines():
        stripped = line.strip()
        if not stripped:
            current_block["lines"].append({"type": "empty", "raw": line})
            continue
        if stripped.startswith("#") or stripped.startswith(";"):
            current_block["lines"].append({"type": "comment", "raw": line})
            continue

        match_section = re.match(r"^\[(.*)\]$", stripped)
        if match_section:
            section_name = match_section.group(1).strip()
            current_block = {"name": section_name, "lines": []}
            blocks.append(current_block)
            current_block["lines"].append({"type": "section", "raw": line})
            continue

        match_kv = re.match(r"^([^=]+)=(.*)$", stripped)
        if match_kv:
            key = match_kv.group(1).strip()
            val = match_kv.group(2).strip()
            current_block["lines"].append(
                {"type": "kv", "raw": line, "key": key, "val": val}
            )
        else:
            current_block["lines"].append({"type": "unknown", "raw": line})

    return blocks, has_trailing_newline, is_crlf


def serialize_ini(blocks: list, has_trailing_newline: bool, is_crlf: bool) -> str:
    lines = []
    for block in blocks:
        for line in block["lines"]:
            lines.append(line["raw"])
    newline = "\r\n" if is_crlf else "\n"
    result = newline.join(lines)
    if has_trailing_newline and result and not result.endswith(newline):
        result += newline
    elif result and not has_trailing_newline and not result.endswith(newline):
        result += "\n"
    return result


def format_val(val) -> str:
    if val is None:
        return ""
    if isinstance(val, bool):
        return "1" if val else "0"
    return str(val)


def normalize_values(val) -> list:
    if isinstance(val, list):
        return [format_val(item) for item in val]
    return [format_val(val)]


def find_block(blocks: list, section_name: str | None):
    return next((block for block in blocks if block["name"] == section_name), None)


def ensure_block(blocks: list, section_name: str | None) -> tuple:
    block = find_block(blocks, section_name)
    if block is not None:
        return block, False

    block = {"name": section_name, "lines": []}
    if section_name is not None:
        if blocks and blocks[-1]["lines"] and blocks[-1]["lines"][-1]["type"] != "empty":
            blocks[-1]["lines"].append({"type": "empty", "raw": ""})
        block["lines"].append({"type": "section", "raw": f"[{section_name}]"})
    blocks.append(block)
    return block, True


def remove_kv_lines(block: dict, key: str) -> bool:
    lower_key = key.lower()
    removed = False
    kept = []
    for line in block["lines"]:
        if line["type"] == "kv" and line["key"].lower() == lower_key:
            removed = True
            continue
        kept.append(line)
    block["lines"] = kept
    return removed


def existing_kv_values(block: dict, key: str) -> list:
    lower_key = key.lower()
    return [
        str(line["val"])
        for line in block["lines"]
        if line["type"] == "kv" and line["key"].lower() == lower_key
    ]


def insert_kv_lines(block: dict, key: str, values: list[str]) -> bool:
    remove_kv_lines(block, key)

    insert_idx = len(block["lines"])
    while insert_idx > 0 and block["lines"][insert_idx - 1]["type"] == "empty":
        insert_idx -= 1

    for value in values:
        block["lines"].insert(
            insert_idx,
            {
                "type": "kv",
                "raw": f"{key}={value}",
                "key": key,
                "val": value,
            },
        )
        insert_idx += 1
    return True


def merge_kv(block: dict, key: str, val) -> bool:
    target_values = normalize_values(val)
    if existing_kv_values(block, key) == target_values:
        return False
    insert_kv_lines(block, key, target_values)
    return True


def merge_section(block: dict, section_settings: dict) -> bool:
    changed = False
    for key, value in section_settings.items():
        if merge_kv(block, key, value):
            changed = True
    return changed


def merge_settings(blocks: list, settings: dict) -> bool:
    if not isinstance(settings, dict):
        return False

    changed = False
    global_settings = {}
    section_settings = {}

    for key, value in settings.items():
        if isinstance(value, dict):
            section_settings[key] = value
        else:
            global_settings[key] = value

    if global_settings:
        block, created = ensure_block(blocks, None)
        changed = created or merge_section(block, global_settings) or changed

    for section_name, pairs in section_settings.items():
        block, created = ensure_block(blocks, section_name)
        changed = created or merge_section(block, pairs) or changed

    return changed


def check_installed(args: dict, request_id: str) -> dict:
    installed = False
    appdata = os.getenv("APPDATA")
    if appdata and os.path.isdir(os.path.join(appdata, "vlc")):
        installed = True
    elif shutil.which("vlc.exe") or shutil.which("vlc"):
        installed = True

    return {
        "requestId": request_id,
        "success": True,
        "changed": False,
        "data": installed,
    }


def apply_config(args: dict, context: dict, request_id: str) -> dict:
    dry_run = context.get("dryRun", False)

    try:
        settings = args.get("settings", {})
        if not isinstance(settings, dict):
            raise ValueError("settings must be an object")

        config_path = get_config_path()
        current_text = read_text(config_path)
        blocks, has_trailing_newline, is_crlf = parse_ini(current_text)

        if not current_text:
            has_trailing_newline = True

        changed = merge_settings(blocks, settings)
        if not changed:
            return {
                "requestId": request_id,
                "success": True,
                "changed": False,
                "data": {"path": config_path},
            }

        new_text = serialize_ini(blocks, has_trailing_newline, is_crlf)

        if dry_run:
            log(f"Would update {config_path} with new settings")
            return {
                "requestId": request_id,
                "success": True,
                "changed": True,
                "data": {"path": config_path, "settings": settings},
            }

        write_text(config_path, new_text)
        log(f"Updated VLC config: {config_path}")
        return {
            "requestId": request_id,
            "success": True,
            "changed": True,
            "data": {"path": config_path},
        }

    except Exception as exc:
        log(f"Failed to apply config: {exc}")
        return {
            "requestId": request_id,
            "success": False,
            "changed": False,
            "error": str(exc),
            "data": None,
        }


def main() -> None:
    input_data = sys.stdin.read()
    if not input_data:
        return

    try:
        request = json.loads(input_data)
    except json.JSONDecodeError as exc:
        log(f"Failed to parse request: {exc}")
        response = {
            "requestId": "unknown",
            "success": False,
            "changed": False,
            "error": f"Failed to parse JSON request: {exc}",
        }
        sys.stdout.write(json.dumps(response) + "\n")
        sys.stdout.flush()
        return

    request_id = request.get("requestId", "unknown")
    command = request.get("command")
    args = request.get("args", {})
    context = request.get("context", {})

    try:
        if command == "check_installed":
            response = check_installed(args, request_id)
        elif command == "apply":
            response = apply_config(args, context, request_id)
        else:
            response = {
                "requestId": request_id,
                "success": False,
                "changed": False,
                "error": f"Unknown command: {command}",
            }
    except Exception as fatal_err:
        response = {
            "requestId": request_id,
            "success": False,
            "changed": False,
            "error": f"Internal Script Error: {fatal_err}",
        }

    sys.stdout.write(json.dumps(response) + "\n")
    sys.stdout.flush()


if __name__ == "__main__":
    main()
