import json
import os
import re
import shutil
import sys


def log(msg):
    sys.stderr.write(f"[openssh-plugin] {msg}\n")
    sys.stderr.flush()


def get_config_path():
    home_dir = os.path.expanduser("~")
    if not home_dir or home_dir == "~":
        home_dir = os.getenv("HOME") or os.getenv("USERPROFILE")
    if not home_dir:
        raise Exception("Could not determine the user's home directory")

    config_dir = os.path.join(home_dir, ".ssh")
    return os.path.join(config_dir, "config")


def read_text(file_path: str) -> str:
    if not os.path.exists(file_path):
        return ""
    try:
        with open(file_path, "r", encoding="utf-8") as f:
            return f.read()
    except Exception as e:
        raise Exception(f"Could not read {file_path}: {e}") from e


def write_text(file_path: str, data: str) -> None:
    os.makedirs(os.path.dirname(file_path), mode=0o700, exist_ok=True)
    tmp_path = file_path + ".tmp"
    with open(tmp_path, "w", encoding="utf-8") as f:
        f.write(data)
    os.chmod(tmp_path, 0o600)
    # Note: On Windows, os.chmod and mode=0o700 are largely no-ops for ACLs.
    # We rely on the user's home directory inheriting secure default ACLs.
    # For full Windows ACL enforcement, icacls or pywin32 would be required.
    os.replace(tmp_path, file_path)


def parse_ssh_config(text: str) -> tuple:
    blocks = []
    current_block = {"name": None, "lines": []}
    blocks.append(current_block)
    has_trailing_newline = text.endswith("\n")

    for line in text.splitlines():
        stripped = line.strip()
        if not stripped:
            current_block["lines"].append({"type": "empty", "raw": line})
            continue
        if stripped.startswith("#"):
            current_block["lines"].append({"type": "comment", "raw": line})
            continue

        match = re.match(r"^([a-zA-Z0-9_-]+)[\s=]+(.*)$", stripped)
        if match:
            key = match.group(1)
            val = match.group(2).strip()
            if key.lower() == "host":
                current_block = {"name": val, "lines": []}
                blocks.append(current_block)
                current_block["lines"].append({"type": "kv", "raw": line, "key": key, "val": val})
            else:
                current_block["lines"].append({"type": "kv", "raw": line, "key": key, "val": val})
        else:
            current_block["lines"].append({"type": "unknown", "raw": line})

    return blocks, has_trailing_newline


def serialize_ssh_config(blocks: list, has_trailing_newline: bool) -> str:
    lines = []
    for b in blocks:
        for line in b["lines"]:
            lines.append(line["raw"])
    res = "\n".join(lines)
    if has_trailing_newline and res and not res.endswith("\n"):
        res += "\n"
    return res


def merge_kv(block: dict, key: str, val: str) -> bool:
    lower_key = key.lower()

    # Check if key exists
    for line in block["lines"]:
        if line["type"] == "kv" and line["key"].lower() == lower_key:
            if str(line["val"]) != str(val):
                # preserve indentation
                indent_match = re.match(r"^(\s+)", line["raw"])
                indent = indent_match.group(1) if indent_match else ""
                if not indent and block["name"] is not None:
                    indent = "    "

                str_val = str(val)
                line["val"] = str_val
                line["raw"] = f"{indent}{key} {str_val}"
                return True
            return False

    # Key not found, append it
    indent = "    " if block["name"] is not None else ""

    # insert before trailing empty lines if any
    insert_idx = len(block["lines"])
    while insert_idx > 0 and block["lines"][insert_idx - 1]["type"] == "empty":
        insert_idx -= 1

    block["lines"].insert(
        insert_idx,
        {
            "type": "kv",
            "raw": f"{indent}{key} {val}",
            "key": key,
            "val": str(val),
        },
    )
    return True


def merge_settings(blocks: list, args: dict) -> bool:
    changed = False

    global_args = args.get("global", {})
    hosts_args = args.get("hosts", {})

    # 1. Merge global
    global_block = blocks[0]
    for k, v in global_args.items():
        if merge_kv(global_block, k, v):
            changed = True

    # 2. Merge hosts
    for host_name, host_settings in hosts_args.items():
        # find host blocks (case-insensitive) while preserving original casing in output
        normalized_host_name = str(host_name).casefold()
        matching_blocks = [
            b for b in blocks if b["name"] is not None and str(b["name"]).casefold() == normalized_host_name
        ]

        if not matching_blocks:
            # create new host block
            new_block = {"name": host_name, "lines": []}

            # ensure previous block ends with empty line for spacing
            if blocks and blocks[-1]["lines"] and blocks[-1]["lines"][-1]["type"] != "empty":
                blocks[-1]["lines"].append({"type": "empty", "raw": ""})

            new_block["lines"].append(
                {
                    "type": "kv",
                    "raw": f"Host {host_name}",
                    "key": "Host",
                    "val": host_name,
                }
            )
            blocks.append(new_block)
            matching_blocks = [new_block]
            changed = True

        for k, v in host_settings.items():
            if k.lower() == "host":
                continue

            # Update key in all blocks where it exists
            key_found = False
            for b in matching_blocks:
                if any(line["type"] == "kv" and line["key"].lower() == k.lower() for line in b["lines"]):
                    if merge_kv(b, k, v):
                        changed = True
                    key_found = True

            # If key didn't exist in any matching block, append to the first one
            if not key_found:
                if merge_kv(matching_blocks[0], k, v):
                    changed = True

    return changed


def check_installed(args: dict, request_id: str) -> dict:
    installed = shutil.which("ssh.exe") is not None or shutil.which("ssh") is not None
    return {
        "requestId": request_id,
        "success": True,
        "changed": False,
        "data": {"installed": installed},
    }


def apply_config(args: dict, context: dict, request_id: str) -> dict:
    dry_run = context.get("dryRun", False)

    try:
        config_path = get_config_path()
        current_text = read_text(config_path)

        blocks, has_trailing_newline = parse_ssh_config(current_text)
        changed = merge_settings(blocks, args)

        if not changed:
            return {
                "requestId": request_id,
                "success": True,
                "changed": False,
            }

        new_text = serialize_ssh_config(blocks, has_trailing_newline)

        if dry_run:
            log(f"Would update {config_path} with new settings")
            return {
                "requestId": request_id,
                "success": True,
                "changed": True,
            }

        write_text(config_path, new_text)
        log(f"Updated SSH config: {config_path}")

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
