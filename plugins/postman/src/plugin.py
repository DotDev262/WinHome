import datetime
import glob
import json
import os
import shutil
import sys
import tempfile
import uuid

CONFIG_FILENAMES = ("settings.json", "userSettings.json", "config.json")
DEFAULT_PACKAGE_DIR = "postman-settings"


def log(msg: str) -> None:
    sys.stderr.write(f"[postman-plugin] {msg}\n")
    sys.stderr.flush()


def get_postman_root() -> str:
    appdata = os.getenv("APPDATA")
    if not appdata:
        raise RuntimeError("APPDATA environment variable not found")
    return os.path.join(appdata, "Postman")


def discover_config_candidates(postman_root: str) -> list[str]:
    candidates: list[str] = []
    packages_dir = os.path.join(postman_root, "packages")

    for filename in CONFIG_FILENAMES:
        pattern = os.path.join(packages_dir, "**", filename)
        candidates.extend(path for path in glob.glob(pattern, recursive=True))

    storage_settings = os.path.join(postman_root, "storage", "settings.json")
    if os.path.isfile(storage_settings):
        candidates.append(storage_settings)

    unique_candidates = []
    seen = set()
    for path in candidates:
        normalized = os.path.normcase(os.path.abspath(path))
        if normalized not in seen:
            seen.add(normalized)
            unique_candidates.append(path)
    return unique_candidates


def discover_config_path() -> str:
    postman_root = get_postman_root()
    candidates = discover_config_candidates(postman_root)
    if candidates:
        return max(candidates, key=os.path.getmtime)

    return os.path.join(
        postman_root,
        "packages",
        DEFAULT_PACKAGE_DIR,
        "settings.json",
    )


def read_json(file_path: str) -> dict:
    if not os.path.exists(file_path):
        return {}

    try:
        with open(file_path, "r", encoding="utf-8") as handle:
            data = json.load(handle)
            return data if isinstance(data, dict) else {}
    except json.JSONDecodeError as exc:
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
        return {}
    except OSError as exc:
        raise OSError(f"Could not read {file_path}: {exc}") from exc


def write_json(file_path: str, data: dict) -> None:
    dir_path = os.path.dirname(file_path)
    if dir_path:
        os.makedirs(dir_path, exist_ok=True)

    fd, temp_path = tempfile.mkstemp(
        prefix="settings.",
        suffix=".tmp",
        dir=dir_path or None,
        text=True,
    )
    try:
        with os.fdopen(fd, "w", encoding="utf-8", newline="\n") as handle:
            json.dump(data, handle, indent=2)
            handle.write("\n")
        os.replace(temp_path, file_path)
    except Exception:
        try:
            os.remove(temp_path)
        except OSError:
            pass
        raise


def deep_merge(target: dict, source: dict) -> bool:
    changed = False
    for key, value in source.items():
        if isinstance(value, dict):
            if key not in target or not isinstance(target.get(key), dict):
                target[key] = {}
                changed = True
            if deep_merge(target[key], value):
                changed = True
        elif key not in target or target[key] != value:
            target[key] = value
            changed = True
    return changed


def check_installed(args: dict, request_id: str) -> dict:
    installed = False
    appdata = os.getenv("APPDATA")
    if appdata and os.path.isdir(os.path.join(appdata, "Postman")):
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

        config_path = discover_config_path()
        current_config = read_json(config_path)
        changed = deep_merge(current_config, settings)

        if not changed:
            return {
                "requestId": request_id,
                "success": True,
                "changed": False,
                "data": {"path": config_path},
            }

        if dry_run:
            log(f"Would update {config_path} with new settings")
            return {
                "requestId": request_id,
                "success": True,
                "changed": True,
                "data": {"path": config_path, "settings": settings},
            }

        write_json(config_path, current_config)
        log(f"Updated Postman config: {config_path}")
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
