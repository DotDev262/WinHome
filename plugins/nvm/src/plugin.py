import json
import os
import shutil
import sys
import tempfile

def get_settings_path():
    appdata = os.environ.get("APPDATA", "")
    return os.path.join(appdata, "nvm", "settings.txt")

def log(msg):
    sys.stderr.write(f"[nvm-plugin] {msg}\n")

def check_installed(args, request_id):
    exists = os.path.exists(get_settings_path()) or shutil.which("nvm.exe") is not None
    return {
        "requestId": request_id,
        "success": True,
        "changed": False,
        "data": exists,
    }

def read_settings_file(path):
    lines = []
    if os.path.exists(path):
        try:
            with open(path, "r", encoding="utf-8") as f:
                lines = f.readlines()
        except Exception as e:
            return None, f"Error reading {path}: {e}"
    return lines, None

def write_settings_file(path, lines):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    fd, temp_path = tempfile.mkstemp(dir=os.path.dirname(path), text=True)
    try:
        with os.fdopen(fd, "w", encoding="utf-8") as f:
            for line in lines:
                f.write(line)
                if not line.endswith("\n"):
                    f.write("\n")
        os.replace(temp_path, path)
        return None
    except Exception as e:
        os.remove(temp_path)
        return f"Error writing settings: {e}"

def apply_config(args, context, request_id):
    dry_run = context.get("dryRun", False)
    settings = args.get("settings", {})
    
    appdata = os.environ.get("APPDATA", "")
    if not appdata:
        return {
            "requestId": request_id,
            "success": False,
            "changed": False,
            "error": "APPDATA is not set.",
        }

    settings_path = get_settings_path()
    lines, error = read_settings_file(settings_path)
    if error:
        return {
            "requestId": request_id,
            "success": False,
            "changed": False,
            "error": error,
        }

    changed = False
    new_lines = []
    found_keys = set()

    for line in lines:
        stripped = line.strip()
        if not stripped or stripped.startswith("#"):
            new_lines.append(line)
            continue
        
        parts = line.split("=", 1)
        if len(parts) == 2:
            key = parts[0].strip()
            # If the key needs updating
            if key in settings:
                found_keys.add(key)
                new_val = str(settings[key])
                current_val = parts[1].strip()
                if current_val != new_val:
                    changed = True
                    new_lines.append(f"{key}={new_val}\n")
                else:
                    new_lines.append(line)
            else:
                new_lines.append(line)
        else:
            new_lines.append(line)

    # Append missing keys
    for key, value in settings.items():
        if key not in found_keys:
            changed = True
            new_lines.append(f"{key}={value}\n")

    if not changed:
        return {
            "requestId": request_id,
            "success": True,
            "changed": False,
        }

    if dry_run:
        log(f"Would update nvm settings at {settings_path}")
        return {
            "requestId": request_id,
            "success": True,
            "changed": True,
        }

    error = write_settings_file(settings_path, new_lines)
    if error:
        return {
            "requestId": request_id,
            "success": False,
            "changed": False,
            "error": error,
        }

    return {
        "requestId": request_id,
        "success": True,
        "changed": True,
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

    command = request.get("command")
    args = request.get("args", {})
    context = request.get("context", {})
    request_id = request.get("requestId", "")

    if command == "apply":
        response = apply_config(args, context, request_id)
    elif command == "check_installed":
        response = check_installed(args, request_id)
    else:
        response = {
            "requestId": request_id,
            "success": False,
            "changed": False,
            "error": f"Unknown command: {command}",
        }

    print(json.dumps(response))

if __name__ == "__main__":
    main()