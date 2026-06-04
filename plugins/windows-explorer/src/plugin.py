import sys
import json
import winreg

REG_PATH = r"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"

def log(msg):
    sys.stderr.write(f"[windows-explorer] {msg}\n")
    sys.stderr.flush()

def read_registry_values():
    values = {}
    try:
        with winreg.OpenKey(winreg.HKEY_CURRENT_USER, REG_PATH, 0, winreg.KEY_READ) as key:
            try:
                i = 0
                while True:
                    name, value, _ = winreg.EnumValue(key, i)
                    values[name] = value
                    i += 1
            except OSError:
                pass
    except FileNotFoundError:
        log(f"Registry key {REG_PATH} not found.")
    except Exception as e:
        log(f"Error reading registry: {e}")
    return values

def check_installed(request_id):
    return {
        "requestId": request_id,
        "success": True,
        "changed": False,
        "data": True
    }

def apply_config(args, context, request_id):
    dry_run = context.get("dryRun", False)
    settings = args.get("settings", {})
    
    if not settings:
        return {
            "requestId": request_id,
            "success": True,
            "changed": False
        }

    current_values = read_registry_values()
    updates = {}

    # Map configuration settings to registry values
    mappings = {
        "HideFileExt": "HideFileExt",
        "ShowSuperHidden": "ShowSuperHidden",
        "ShowSyncProviderNotification": "ShowSyncProviderNotification",
        "ShowStatusBar": "ShowStatusBar",
        "AutoCheckSelect": "AutoCheckSelect",
        "DisableThumbnails": "DisableThumbnails",
        "DisableThumbsDBOnNetworkFolders": "DisableThumbsDBOnNetworkFolders",
        "SeparateProcess": "SeparateProcess"
    }

    for config_key, reg_key in mappings.items():
        if config_key in settings:
            val = settings[config_key]
            # Convert bool to 1 or 0 for DWORD
            expected = 1 if val else 0
            if current_values.get(reg_key) != expected:
                updates[reg_key] = expected

    # Special handling for 'Hidden'
    if "Hidden" in settings:
        hidden_val = settings["Hidden"]
        if hidden_val in (1, 2):
            if current_values.get("Hidden") != hidden_val:
                updates["Hidden"] = hidden_val
        else:
            log(f"Invalid value for Hidden: {hidden_val}. Must be 1 or 2.")

    changed = len(updates) > 0

    if dry_run:
        for k, v in updates.items():
            log(f"Dry run: Would update registry key {k} to {v}")
    elif changed:
        try:
            with winreg.OpenKey(winreg.HKEY_CURRENT_USER, REG_PATH, 0, winreg.KEY_SET_VALUE) as key:
                for k, v in updates.items():
                    winreg.SetValueEx(key, k, 0, winreg.REG_DWORD, v)
                    log(f"Updated registry key {k} to {v}")
        except Exception as e:
            return {
                "requestId": request_id,
                "success": False,
                "changed": False,
                "error": f"Failed to write to registry: {e}"
            }

    return {
        "requestId": request_id,
        "success": True,
        "changed": changed
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
        response = check_installed(request_id)
    else:
        response = {
            "requestId": request_id,
            "success": False,
            "changed": False,
            "error": f"Unknown command: {command}"
        }

    # Use \n as trailing newline instead of \r\n explicitly
    sys.stdout.write(json.dumps(response) + "\n")
    sys.stdout.flush()

if __name__ == "__main__":
    main()
