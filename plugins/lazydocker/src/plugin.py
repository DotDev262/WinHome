# /// script
# requires-python = ">=3.10"
# dependencies = [
#     "pyyaml",
# ]
# ///

import copy
import json
import os
import shutil
import sys
import tempfile
import uuid

try:
    import yaml
except ImportError:
    pass


def deep_merge(dict1, dict2):
    """
    Deep merges dict2 into dict1.
    Returns a tuple (merged_dict, changed_bool)
    """
    merged = copy.deepcopy(dict1)
    changed = False

    for key, value in dict2.items():
        if key in merged and isinstance(merged[key], dict) and isinstance(value, dict):
            # Recurse
            sub_merged, sub_changed = deep_merge(merged[key], value)
            merged[key] = sub_merged
            if sub_changed:
                changed = True
        else:
            if key not in merged or merged[key] != value:
                merged[key] = copy.deepcopy(value)
                changed = True

    return merged, changed


def get_config_path():
    appdata = os.environ.get("APPDATA", "")
    if not appdata:
        # Fallback for testing if APPDATA is not set
        appdata = os.path.expanduser("~\\AppData\\Roaming")
    return os.path.join(appdata, "lazydocker", "config.yml")


def check_installed(args, request_id):
    installed = shutil.which("lazydocker.exe") is not None
    # On linux/mac for testing it might be 'lazydocker'
    if not installed:
        installed = shutil.which("lazydocker") is not None

    return {"requestId": request_id, "success": True, "changed": False, "data": installed}


def apply_config(args, context, request_id):
    settings = args.get("settings", {})
    if not settings:
        return {"requestId": request_id, "success": True, "changed": False, "data": None}

    config_path = get_config_path()
    dry_run = context.get("dryRun", False)

    # Read existing config
    existing_config = {}
    if os.path.exists(config_path):
        try:
            with open(config_path, "r", encoding="utf-8") as f:
                parsed = yaml.safe_load(f)
                if isinstance(parsed, dict):
                    existing_config = parsed
        except Exception as e:
            sys.stderr.write(
                "[lazydocker-plugin] Warning: Failed to parse "
                f"existing config ({str(e)}). Backing up and starting fresh.\n"
            )

            # Backup corrupted config
            backup_path = f"{config_path}.{uuid.uuid4().hex[:8]}.bak"
            try:
                shutil.copy2(config_path, backup_path)
            except Exception as backup_e:
                sys.stderr.write(f"[lazydocker-plugin] Warning: Failed to create backup: {str(backup_e)}\n")

    # Deep merge
    merged_config, changed = deep_merge(existing_config, settings)

    if not changed:
        return {"requestId": request_id, "success": True, "changed": False, "data": None}

    if not dry_run:
        try:
            config_dir = os.path.dirname(config_path)
            if not os.path.exists(config_dir):
                os.makedirs(config_dir, mode=0o700, exist_ok=True)

            fd, temp_path = tempfile.mkstemp(prefix="lazydocker-", suffix=".tmp", dir=os.path.dirname(config_path))
            try:
                with os.fdopen(fd, "w", encoding="utf-8") as f:
                    yaml.dump(merged_config, f, default_flow_style=False, sort_keys=False)
                os.replace(temp_path, config_path)
            except BaseException:
                os.unlink(temp_path)
                raise
            sys.stderr.write(f"[lazydocker-plugin] Updated config at {config_path}\n")
        except Exception as e:
            return {
                "requestId": request_id,
                "success": False,
                "changed": False,
                "data": None,
                "error": f"Failed to write config: {str(e)}",
            }
    else:
        sys.stderr.write(f"[lazydocker-plugin] Would update {config_path} with new merged config\n")

    return {"requestId": request_id, "success": True, "changed": True, "data": None}


def main():
    try:
        raw_input = sys.stdin.read().strip()
        if not raw_input:
            print(
                json.dumps(
                    {
                        "requestId": "unknown",
                        "success": False,
                        "changed": False,
                        "data": None,
                        "error": "Empty input received via stdin",
                    }
                )
            )
            sys.stdout.flush()
            return

        request = json.loads(raw_input)
        command = request.get("command")
        args = request.get("args", {})
        context = request.get("context", {})
        request_id = request.get("requestId", "unknown")

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
                    "data": None,
                    "error": f"Unknown command: {command}",
                }
        except Exception as inner_e:
            sys.stderr.write(f"[lazydocker-plugin] Command Error: {str(inner_e)}\n")
            response = {
                "requestId": request_id,
                "success": False,
                "changed": False,
                "data": None,
                "error": f"Internal Script Error: {str(inner_e)}",
            }

        print(json.dumps(response))
        sys.stdout.flush()
    except Exception as e:
        sys.stderr.write(f"[lazydocker-plugin] Error: {str(e)}\n")
        print(
            json.dumps(
                {
                    "requestId": "unknown",
                    "success": False,
                    "changed": False,
                    "data": None,
                    "error": f"Plugin crashed: {str(e)}",
                }
            )
        )


if __name__ == "__main__":
    main()
