import json
import sys

try:
    import winreg
except ImportError:
    winreg = None

REG_PATH = r"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"

BOOL_KEYS = frozenset(
    {
        "HideFileExt",
        "ShowSuperHidden",
        "ShowSyncProviderNotification",
        "ShowStatusBar",
        "AutoCheckSelect",
        "DisableThumbnails",
        "DisableThumbsDBOnNetworkFolders",
        "SeparateProcess",
    }
)

SUPPORTED_KEYS = BOOL_KEYS | {"Hidden"}


def log(message: str) -> None:
    sys.stderr.write(f"[windows-explorer-plugin] {message}\n")
    sys.stderr.flush()


def registry_bool_to_python(value: int) -> bool:
    return bool(value)


def python_bool_to_registry(value: bool) -> int:
    return 1 if value else 0


def normalize_registry_value(key: str, value, val_type: int):
    if key == "Hidden":
        return int(value)

    if key in BOOL_KEYS:
        if val_type == 4:
            return registry_bool_to_python(int(value))
        return bool(value)

    if val_type == 4:
        return int(value)
    return value


def read_settings() -> dict:
    settings: dict = {}
    if winreg is None:
        log("winreg module not available.")
        return settings

    try:
        with winreg.OpenKey(
            winreg.HKEY_CURRENT_USER, REG_PATH, 0, winreg.KEY_READ
        ) as key:
            index = 0
            while True:
                try:
                    name, value, val_type = winreg.EnumValue(key, index)
                    if name in SUPPORTED_KEYS:
                        settings[name] = normalize_registry_value(
                            name, value, val_type
                        )
                    index += 1
                except OSError:
                    break
    except FileNotFoundError:
        pass
    except Exception as error:
        log(f"Error reading registry: {error}")

    return settings


def validate_setting(key: str, value) -> str | None:
    if key not in SUPPORTED_KEYS:
        return f"Unsupported setting: {key}"

    if key == "Hidden":
        if not isinstance(value, int) or value not in (1, 2):
            return f"Invalid Hidden: {value}. Must be 1 (hide) or 2 (show)."
        return None

    if not isinstance(value, bool):
        return f"Invalid {key}: {value}. Must be a boolean."
    return None


def registry_value_for_write(key: str, value):
    if key == "Hidden":
        return 4, int(value)
    return 4, python_bool_to_registry(bool(value))


def check_installed(_args: dict, request_id: str) -> dict:
    return {
        "requestId": request_id,
        "success": True,
        "changed": False,
        "data": True,
    }


def apply_config(args: dict, context: dict, request_id: str) -> dict:
    dry_run = bool(context.get("dryRun", False))
    desired = args.get("settings", {})

    if not isinstance(desired, dict):
        return {
            "requestId": request_id,
            "success": False,
            "changed": False,
            "error": "settings must be a dictionary",
        }

    if not desired:
        return {
            "requestId": request_id,
            "success": True,
            "changed": False,
        }

    for key, value in desired.items():
        error = validate_setting(key, value)
        if error:
            return {
                "requestId": request_id,
                "success": False,
                "changed": False,
                "error": error,
            }

    current = read_settings()
    to_update: dict = {}

    for key, value in desired.items():
        if current.get(key) != value:
            to_update[key] = value

    if not to_update:
        return {
            "requestId": request_id,
            "success": True,
            "changed": False,
        }

    if dry_run:
        log(f"Dry run: would update registry values: {to_update}")
        return {
            "requestId": request_id,
            "success": True,
            "changed": True,
        }

    if winreg is None:
        return {
            "requestId": request_id,
            "success": False,
            "changed": False,
            "error": "winreg module not available on this platform.",
        }

    try:
        with winreg.CreateKeyEx(
            winreg.HKEY_CURRENT_USER, REG_PATH, 0, winreg.KEY_SET_VALUE
        ) as key:
            for name, value in to_update.items():
                reg_type, reg_value = registry_value_for_write(name, value)
                winreg.SetValueEx(key, name, 0, reg_type, reg_value)
                log(f"Updated registry: {name} = {reg_value}")

        return {
            "requestId": request_id,
            "success": True,
            "changed": True,
        }
    except Exception as error:
        return {
            "requestId": request_id,
            "success": False,
            "changed": False,
            "error": f"Failed to write to registry: {error}",
        }


def handle(request: dict) -> dict:
    request_id = request.get("requestId", "unknown")
    command = request.get("command")
    args = request.get("args", {})
    context = request.get("context", {})

    if command == "check_installed":
        return check_installed(args, request_id)
    if command == "apply":
        if not isinstance(args, dict):
            return {
                "requestId": request_id,
                "success": False,
                "changed": False,
                "error": "args must be an object",
            }
        if not isinstance(context, dict):
            return {
                "requestId": request_id,
                "success": False,
                "changed": False,
                "error": "context must be an object",
            }
        return apply_config(args, context, request_id)

    return {
        "requestId": request_id,
        "success": False,
        "changed": False,
        "error": f"Unknown command: {command}",
    }


def main() -> None:
    raw = sys.stdin.read()
    if not raw or not raw.strip():
        sys.stdout.write(
            json.dumps(
                {
                    "requestId": "unknown",
                    "success": False,
                    "changed": False,
                    "error": "Empty input",
                }
            )
            + "\n"
        )
        sys.stdout.flush()
        return

    try:
        request = json.loads(raw)
        result = handle(request)
    except json.JSONDecodeError as error:
        result = {
            "requestId": "unknown",
            "success": False,
            "changed": False,
            "error": f"Failed to parse request: {error}",
        }
    except Exception as error:
        request_id = "unknown"
        if "request" in locals() and isinstance(request, dict):
            request_id = request.get("requestId", "unknown")
        result = {
            "requestId": request_id,
            "success": False,
            "changed": False,
            "error": str(error),
        }

    sys.stdout.write(json.dumps(result) + "\n")
    sys.stdout.flush()


if __name__ == "__main__":
    main()
