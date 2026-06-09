import json
import sys
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


def check_installed():
    try:
        with winreg.OpenKey(winreg.HKEY_CURRENT_USER, REG_PATH, 0, winreg.KEY_READ):
            return True
    except FileNotFoundError:
        return False
    except OSError:
        return False


def apply_config(args):
    dry_run = args.get("dryRun", False)
    settings = args.get("settings", {})

    if not settings:
        return {"changed": False}

    current_values = read_registry_values()
    updates = {}

    mappings = {
        "HideFileExt": "HideFileExt",
        "ShowSuperHidden": "ShowSuperHidden",
        "ShowSyncProviderNotification": "ShowSyncProviderNotification",
        "ShowStatusBar": "ShowStatusBar",
        "AutoCheckSelect": "AutoCheckSelect",
        "DisableThumbnails": "DisableThumbnails",
        "DisableThumbsDBOnNetworkFolders": "DisableThumbsDBOnNetworkFolders",
        "SeparateProcess": "SeparateProcess",
    }

    for config_key, reg_key in mappings.items():
        if config_key in settings:
            val = settings[config_key]
            expected = 1 if val else 0
            if current_values.get(reg_key) != expected:
                updates[reg_key] = expected

    if "Hidden" in settings:
        hidden_val = settings["Hidden"]
        if hidden_val in (1, 2):
            if current_values.get("Hidden") != hidden_val:
                updates["Hidden"] = hidden_val
        else:
            return {"error": f"Invalid value for Hidden: {hidden_val}. Must be 1 or 2."}

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
            return {"error": f"Failed to write to registry: {e}"}

    return {"changed": changed}


def main():
    input_data = sys.stdin.read()
    if not input_data.strip():
        response = {"requestId": "unknown", "error": "No input received"}
        sys.stdout.write(json.dumps(response) + "\n")
        sys.stdout.flush()
        return

    try:
        request = json.loads(input_data)
    except Exception as e:
        response = {"requestId": "unknown", "error": f"Failed to parse JSON request: {e}"}
        sys.stdout.write(json.dumps(response) + "\n")
        sys.stdout.flush()
        return

    request_id = request.get("requestId") or "unknown"
    command = request.get("command")
    args = request.get("args", {})

    response = {"requestId": request_id}

    if command == "apply":
        apply_res = apply_config(args)
        response.update(apply_res)
    elif command == "check_installed":
        result = check_installed()
        response["installed"] = result
    else:
        response["error"] = f"Unknown command: {command}"

    sys.stdout.write(json.dumps(response) + "\n")
    sys.stdout.flush()


if __name__ == "__main__":
    main()
