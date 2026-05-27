import json
import os
import re
import shutil
import sys


def log(msg):
    sys.stderr.write(f"[rclone-plugin] {msg}\n")
    sys.stderr.flush()


def get_config_path():
    userprofile = os.getenv("USERPROFILE")
    if not userprofile:
        raise Exception("USERPROFILE environment variable not found")
    return os.path.join(userprofile, ".config", "rclone", "rclone.conf")


def read_text(file_path: str) -> str:
    if not os.path.exists(file_path):
        return ""
    try:
        with open(file_path, "r", encoding="utf-8") as f:
            return f.read()
    except OSError as e:
        raise OSError(f"Could not read {file_path}: {e}") from e


def write_text(file_path: str, data: str) -> None:
    os.makedirs(os.path.dirname(file_path), mode=0o700, exist_ok=True)
    tmp_path = file_path + ".tmp"
    with open(tmp_path, "w", encoding="utf-8") as f:
        f.write(data)
    os.replace(tmp_path, file_path)


def parse_ini(text: str) -> tuple:
    blocks = []
    current_block = {"name": None, "lines": []}
    blocks.append(current_block)
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

        # Section header
        match_section = re.match(r"^\[(.*)\]$", stripped)
        if match_section:
            section_name = match_section.group(1).strip()
            current_block = {"name": section_name, "lines": []}
            blocks.append(current_block)
            current_block["lines"].append({"type": "section", "raw": line})
            continue

        # Key = Value
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


def serialize_ini(
    blocks: list, has_trailing_newline: bool, is_crlf: bool
) -> str:
    lines = []
    for b in blocks:
        for line in b["lines"]:
            lines.append(line["raw"])
    newline = "\r\n" if is_crlf else "\n"
    res = newline.join(lines)
    if has_trailing_newline and res and not res.endswith(newline):
        res += newline
    return res


def merge_kv(block: dict, key: str, val: str) -> bool:
    lower_key = key.lower()

    for line in block["lines"]:
        if line["type"] == "kv" and line["key"].lower() == lower_key:
            if str(line["val"]) != str(val):
                indent_match = re.match(r"^(\s*)", line["raw"])
                indent = indent_match.group(1) if indent_match else ""

                eq_match = re.search(r"(\s*=\s*)", line["raw"])
                eq_str = eq_match.group(1) if eq_match else " = "

                str_val = str(val)
                line["val"] = str_val
                original_key = line.get("key", key)
                line["raw"] = f"{indent}{original_key}{eq_str}{str_val}"
                return True
            return False

    # Key not found, append it
    insert_idx = len(block["lines"])
    while insert_idx > 0 and block["lines"][insert_idx - 1]["type"] == "empty":
        insert_idx -= 1

    block["lines"].insert(
        insert_idx,
        {"type": "kv", "raw": f"{key} = {val}", "key": key, "val": str(val)},
    )
    return True


def merge_settings(blocks: list, args: dict) -> bool:
    changed = False

    settings = args.get("settings", {})
    remotes = args.get("remotes", {})

    if settings:
        global_block = next((b for b in blocks if b["name"] is None), None)
        if not global_block:
            global_block = {"name": None, "lines": []}
            blocks.insert(0, global_block)
        for k, v in settings.items():
            if merge_kv(global_block, k, v):
                changed = True

    for remote_name, remote_settings in remotes.items():
        block = next((b for b in blocks if b["name"] == remote_name), None)

        if not block:
            block = {"name": remote_name, "lines": []}

            if (
                blocks
                and blocks[-1]["lines"]
                and blocks[-1]["lines"][-1]["type"] != "empty"
            ):
                blocks[-1]["lines"].append({"type": "empty", "raw": ""})

            block["lines"].append(
                {"type": "section", "raw": f"[{remote_name}]"}
            )
            blocks.append(block)
            changed = True

        for k, v in remote_settings.items():
            if merge_kv(block, k, v):
                changed = True

    return changed


def check_installed(args: dict, request_id: str) -> dict:
    installed = False
    if shutil.which("rclone.exe") or shutil.which("rclone"):
        installed = True
    else:
        program_files = os.getenv("PROGRAMFILES", "C:\\Program Files")
        if os.path.exists(os.path.join(program_files, "rclone", "rclone.exe")):
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
        config_path = get_config_path()
        current_text = read_text(config_path)

        blocks, has_trailing_newline, is_crlf = parse_ini(current_text)
        changed = merge_settings(blocks, args)

        if not changed:
            return {
                "requestId": request_id,
                "success": True,
                "changed": False,
            }

        new_text = serialize_ini(blocks, has_trailing_newline, is_crlf)

        if dry_run:
            log(f"Would update {config_path} with new settings")
            return {
                "requestId": request_id,
                "success": True,
                "changed": True,
            }

        write_text(config_path, new_text)
        log(f"Updated Rclone config: {config_path}")

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
    except json.JSONDecodeError as e:
        log(f"Failed to parse request: {e}")
        response = {
            "requestId": "unknown",
            "success": False,
            "changed": False,
            "error": f"Failed to parse JSON request: {str(e)}",
        }
        sys.stdout.write(json.dumps(response) + "\n")
        sys.stdout.flush()
        return
    except Exception as e:
        log(f"Failed to parse request: {e}")
        response = {
            "requestId": "unknown",
            "success": False,
            "changed": False,
            "error": f"Failed to parse request: {str(e)}",
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
