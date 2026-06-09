import datetime
import json
import os
import shutil
import subprocess
import sys
import uuid

# On non-Windows platforms (like CI environments), winreg doesn't exist.
# Let's import it gracefully to allow testing and cross-platform compatibility.
try:
    import winreg
except ImportError:
    winreg = None

REG_PATH = r"Software\7-Zip"

KEY_TYPES = {
    "CompressionLevel": 4,  # winreg.REG_DWORD
    "CompressionMethod": 1,  # winreg.REG_SZ
    "EncryptHeaders": 4,  # winreg.REG_DWORD
    "ContextMenu": 4,  # winreg.REG_DWORD
    "InstallDir": 1,  # winreg.REG_SZ
}


def log(msg):
    sys.stderr.write(f"[7-zip-plugin] {msg}\n")
    sys.stderr.flush()


def _backup_corrupt_registry(reason):
    timestamp = datetime.datetime.now(datetime.timezone.utc).strftime("%Y%m%d%H%M%S")
    suffix = uuid.uuid4().hex[:8]
    user_profile = os.getenv("USERPROFILE") or os.path.expanduser("~")
    backup_path = os.path.join(user_profile, f"7zip_registry.corrupted.{timestamp}.{suffix}.reg")
    log(f"Registry read failed ({reason}). Backing up HKCU\\{REG_PATH} to {backup_path}")
    try:
        subprocess.run(["reg.exe", "export", f"HKCU\\{REG_PATH}", backup_path, "/y"], capture_output=True, check=True)
    except Exception as backup_e:
        log(f"Failed to backup registry key: {backup_e}")


def read_settings():
    settings = {}
    if winreg is None:
        log("winreg module not available.")
        return settings

    try:
        with winreg.OpenKey(winreg.HKEY_CURRENT_USER, REG_PATH, 0, winreg.KEY_READ) as key:
            i = 0
            while True:
                try:
                    name, value, val_type = winreg.EnumValue(key, i)
                    if name in KEY_TYPES:
                        if KEY_TYPES[name] == 4:  # REG_DWORD
                            if name == "EncryptHeaders":
                                settings[name] = bool(value)
                            else:
                                settings[name] = int(value)
                        else:
                            settings[name] = str(value)
                    else:
                        # Fallback parsing for unknown keys
                        if val_type == 4:
                            settings[name] = int(value)
                        elif val_type in (1, 2):  # REG_SZ, REG_EXPAND_SZ
                            settings[name] = str(value)
                        else:
                            settings[name] = value
                    i += 1
                except OSError:
                    break
    except FileNotFoundError:
        # Key doesn't exist yet, return empty dict
        pass
    except Exception as e:
        log(f"Error reading registry: {e}")
        _backup_corrupt_registry(e)
    return settings


def check_installed(args, request_id):
    is_installed = False

    # Check PATH first
    if shutil.which("7z.exe") or shutil.which("7z"):
        is_installed = True
    elif winreg is not None:
        # Check registry key
        try:
            with winreg.OpenKey(winreg.HKEY_CURRENT_USER, REG_PATH, 0, winreg.KEY_READ):
                is_installed = True
        except FileNotFoundError:
            pass
        except Exception as e:
            log(f"Error checking registry: {e}")

    return {"requestId": request_id, "success": True, "changed": False, "data": is_installed}


def apply_config(args, context, request_id):
    dry_run = context.get("dryRun", False)
    desired = args.get("settings", {})
    if not isinstance(desired, dict):
        return {"requestId": request_id, "success": False, "changed": False, "error": "settings must be a dictionary"}

    current = read_settings()
    changed = False
    to_update = {}

    for key, val in desired.items():
        # Validate values
        if key == "CompressionLevel":
            if not isinstance(val, int) or not (0 <= val <= 9):
                return {
                    "requestId": request_id,
                    "success": False,
                    "changed": False,
                    "error": f"Invalid CompressionLevel: {val}. Must be an integer between 0 and 9.",
                }
        elif key == "CompressionMethod":
            if not isinstance(val, str):
                return {
                    "requestId": request_id,
                    "success": False,
                    "changed": False,
                    "error": f"Invalid CompressionMethod: {val}. Must be a string.",
                }
        elif key == "EncryptHeaders":
            if not isinstance(val, bool):
                return {
                    "requestId": request_id,
                    "success": False,
                    "changed": False,
                    "error": f"Invalid EncryptHeaders: {val}. Must be a boolean.",
                }
        elif key == "ContextMenu":
            if not isinstance(val, int):
                return {
                    "requestId": request_id,
                    "success": False,
                    "changed": False,
                    "error": f"Invalid ContextMenu: {val}. Must be an integer.",
                }
        elif key == "InstallDir":
            if not isinstance(val, str):
                return {
                    "requestId": request_id,
                    "success": False,
                    "changed": False,
                    "error": f"Invalid InstallDir: {val}. Must be a string.",
                }

        # Compare values
        if current.get(key) != val:
            to_update[key] = val
            changed = True

    if not changed:
        return {"requestId": request_id, "success": True, "changed": False}

    if dry_run:
        log(f"Dry Run: Would update registry values: {to_update}")
        return {"requestId": request_id, "success": True, "changed": True}

    if winreg is None:
        return {
            "requestId": request_id,
            "success": False,
            "changed": False,
            "error": "winreg module not available on this platform.",
        }

    try:
        with winreg.CreateKeyEx(winreg.HKEY_CURRENT_USER, REG_PATH, 0, winreg.KEY_SET_VALUE) as key:
            for k, v in to_update.items():
                reg_type = KEY_TYPES.get(k)
                if reg_type is None:
                    if isinstance(v, bool):
                        reg_type = 4  # REG_DWORD
                        reg_val = 1 if v else 0
                    elif isinstance(v, int):
                        reg_type = 4  # REG_DWORD
                        reg_val = v
                    else:
                        reg_type = 1  # REG_SZ
                        reg_val = str(v)
                else:
                    if reg_type == 4:
                        if isinstance(v, bool):
                            reg_val = 1 if v else 0
                        else:
                            reg_val = int(v)
                    else:
                        reg_val = str(v)

                winreg.SetValueEx(key, k, 0, reg_type, reg_val)
                log(f"Updated registry: {k} = {reg_val}")

        return {"requestId": request_id, "success": True, "changed": True}
    except Exception as e:
        return {
            "requestId": request_id,
            "success": False,
            "changed": False,
            "error": f"Failed to write to registry: {e}",
        }


def main():
    input_data = sys.stdin.read()
    if not input_data or not input_data.strip():
        response = {
            "requestId": "unknown",
            "success": False,
            "changed": False,
            "error": "Empty input",
            "data": None,
        }
        sys.stdout.write(json.dumps(response) + "\n")
        sys.stdout.flush()
        return

    try:
        request = json.loads(input_data)
    except Exception as e:
        log(f"Failed to parse request: {e}")
        response = {
            "requestId": "unknown",
            "success": False,
            "changed": False,
            "error": f"Failed to parse JSON request: {str(e)}",
            "data": None,
        }
        sys.stdout.write(json.dumps(response) + "\n")
        sys.stdout.flush()
        return

    command = request.get("command")
    args = request.get("args", {})
    context = request.get("context", {})
    request_id = request.get("requestId", "")

    if command == "apply":
        response = apply_config(args, context, request_id)
    elif command == "check_installed":
        response = check_installed(args, request_id)
    else:
        response = {"requestId": request_id, "success": False, "changed": False, "error": f"Unknown command: {command}"}

    sys.stdout.write(json.dumps(response) + "\n")
    sys.stdout.flush()


if __name__ == "__main__":
    main()
