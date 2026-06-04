import configparser
import json
import os
import shutil
import sys
import tempfile
import uuid


def log(msg: str) -> None:
    print(f"[IrfanView Plugin] {msg}", file=sys.stderr)


def get_irfanview_dir() -> str:
    appdata = os.environ.get("APPDATA", "")
    return os.path.join(appdata, "IrfanView") if appdata else ""


def check_installed() -> bool:
    # Check APPDATA
    iv_dir = get_irfanview_dir()
    if iv_dir and os.path.isdir(iv_dir):
        return True

    # Check PATH
    if shutil.which("i_view32.exe") or shutil.which("i_view64.exe"):
        return True
    return False


def get_ini_path() -> str:
    iv_dir = get_irfanview_dir()
    if not iv_dir:
        return ""

    if os.path.isdir(iv_dir):
        # Look for any i_view*.ini
        for f in os.listdir(iv_dir):
            if f.lower().startswith("i_view") and f.lower().endswith(".ini"):
                return os.path.join(iv_dir, f)

    # Default fallback
    return os.path.join(iv_dir, "i_view64.ini")


def apply_settings(settings: dict, dry_run: bool) -> bool:
    if not isinstance(settings, dict):
        log("Settings must be a dictionary")
        return False

    iv_dir = get_irfanview_dir()
    if not iv_dir:
        log("APPDATA not set, cannot locate IrfanView directory.")
        return False

    ini_path = get_ini_path()
    if not ini_path:
        # Default fallback
        ini_path = os.path.join(iv_dir, "i_view64.ini")

    parser = configparser.ConfigParser(interpolation=None, strict=False)
    # Preserve case
    parser.optionxform = str

    if os.path.exists(ini_path):
        try:
            parser.read(ini_path, encoding="utf-8")
        except Exception as e:
            log(f"Failed to parse INI file at {ini_path}: {e}")
            backup_path = f"{ini_path}.{uuid.uuid4()}"
            try:
                shutil.copy2(ini_path, backup_path)
                log(f"Created corruption backup at {backup_path}")
            except Exception as backup_err:
                log(f"Failed to create backup: {backup_err}")

    changed = False
    # Deep merge
    for section, keys in settings.items():
        if not isinstance(keys, dict):
            continue
        if not parser.has_section(section):
            parser.add_section(section)
            changed = True
        for k, v in keys.items():
            str_k = str(k)
            str_v = "1" if isinstance(v, bool) and v else ("0" if isinstance(v, bool) else str(v))
            if not parser.has_option(section, str_k) or parser.get(section, str_k) != str_v:
                parser.set(section, str_k, str_v)
                changed = True

    if not changed:
        return False

    if dry_run:
        log(f"Would write to {ini_path} (dry-run)")
        return True

    os.makedirs(os.path.dirname(ini_path), exist_ok=True)
    fd, temp_path = tempfile.mkstemp(dir=os.path.dirname(ini_path), prefix="i_view.ini.")
    try:
        with os.fdopen(fd, "w", encoding="utf-8", newline="\n") as f:
            parser.write(f, space_around_delimiters=False)
        os.replace(temp_path, ini_path)
        log(f"Successfully wrote to {ini_path}")
        return True
    except Exception as e:
        os.remove(temp_path)
        raise e


def handle_request(request: dict) -> dict:
    req_id = request.get("requestId", "")
    cmd = request.get("command", "")

    if cmd == "check_installed":
        installed = check_installed()
        return {"requestId": req_id, "success": True, "changed": False, "data": installed}
    elif cmd == "apply":
        args = request.get("args", {})
        settings = args.get("settings", {})
        context = request.get("context", {})
        dry_run = context.get("dryRun", False)

        try:
            changed = apply_settings(settings, dry_run)
            return {"requestId": req_id, "success": True, "changed": bool(changed), "data": None}
        except Exception as e:
            return {"requestId": req_id, "success": False, "changed": False, "error": str(e)}

    return {"requestId": req_id, "success": False, "changed": False, "error": f"Unknown command: {cmd}"}


def main():
    if not sys.stdin.isatty():
        input_data = sys.stdin.read().strip()
        if not input_data:
            print(json.dumps({"error": "Empty input"}))
            return

        try:
            request = json.loads(input_data)
        except json.JSONDecodeError as e:
            print(json.dumps({"error": f"Invalid JSON: {e}"}))
            return

        response = handle_request(request)
        print(json.dumps(response))


if __name__ == "__main__":
    main()
