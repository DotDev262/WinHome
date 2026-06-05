import os
import shutil
import json
import sys
import uuid
import tempfile

try:
    import tomllib
except ImportError:
    import tomli as tomllib

def check_installed():
    return shutil.which("rustup") is not None or shutil.which("rustup.exe") is not None

def deep_merge(source, destination):
    for key, value in source.items():
        if isinstance(value, dict):
            node = destination.setdefault(key, {})
            deep_merge(value, node)
        else:
            destination[key] = value
    return destination

def process_request():
    raw_input = sys.stdin.read().strip()
    if not raw_input:
        print(json.dumps({
            "requestId": "unknown",
            "error": "Empty stdin configuration request received"
        }))
        return

    try:
        request_payload = json.loads(raw_input)
    except json.JSONDecodeError:
        print(json.dumps({
            "requestId": "unknown",
            "error": "Invalid JSON formatting in configuration payload"
        }))
        return

    request_id = request_payload.get("requestId")
    if request_id is None:
        request_id = "unknown"
        
    command = request_payload.get("command", "")
    args = request_payload.get("args", {})
    
    if command == "check_installed":
        print(json.dumps({
            "requestId": request_id,
            "installed": check_installed()
        }))
        return

    home_dir = os.path.expanduser("~")
    config_path = os.path.join(home_dir, ".rustup", "settings.toml")
    
    new_settings = args.get("settings", {})
    dry_run = args.get("dryRun", False)

    existing_config = {}
    if os.path.exists(config_path):
        try:
            with open(config_path, "rb") as f:
                existing_config = tomllib.load(f)
        except Exception:
            backup_path = f"{config_path}.{uuid.uuid4()}.bak"
            try:
                shutil.copy2(config_path, backup_path)
            except IOError:
                pass

    merged_config = deep_merge(new_settings, existing_config)

    output_lines = []
    overrides = merged_config.pop("overrides", {})
    
    for k, v in merged_config.items():
        if isinstance(v, str):
            output_lines.append(f'{k} = "{v}"')
        else:
            output_lines.append(f'{k} = {str(v).lower() if isinstance(v, bool) else v}')
            
    if overrides:
        output_lines.append("\n[overrides]")
        for k, v in overrides.items():
            output_lines.append(f'{k} = "{v}"')
            
    final_toml_content = "\n".join(output_lines) + "\n"

    if dry_run:
        print(json.dumps({
            "requestId": request_id,
            "installed": check_installed(),
            "status": "dry_run",
            "updated": True
        }))
        return

    try:
        os.makedirs(os.path.dirname(config_path), exist_ok=True)
        fd, temp_path = tempfile.mkstemp(dir=os.path.dirname(config_path))
        with os.fdopen(fd, "w", encoding="utf-8") as tmp_file:
            tmp_file.write(final_toml_content)
        os.replace(temp_path, config_path)
    except Exception as err:
        print(json.dumps({
            "requestId": request_id,
            "error": f"Failed executing atomic write operations: {str(err)}"
        }))
        return

    print(json.dumps({
        "requestId": request_id,
        "installed": check_installed(),
        "status": "success",
        "written": True
    }))

if __name__ == "__main__":
    process_request()
