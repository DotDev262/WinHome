import configparser
import json
import os
import shutil
import sys
import tempfile


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


def apply_settings(settings: dict, dry_run: bool) -> None:
    iv_dir = get_irfanview_dir()
    if not iv_dir:
        log("APPDATA not set, cannot locate IrfanView directory.")
        return

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
            # Continue with empty config, existing file will be overwritten

    # Deep merge
    for section, keys in settings.items():
        if not isinstance(keys, dict):
            continue
        if not parser.has_section(section):
            parser.add_section(section)
        for k, v in keys.items():
            if isinstance(v, bool):
                # Convert bool to 1 or 0 (common in IrfanView)
                parser.set(section, str(k), "1" if v else "0")
            elif isinstance(v, (int, float)):
                parser.set(section, str(k), str(v))
            else:
                parser.set(section, str(k), str(v))

    if dry_run:
        log(f"Would write to {ini_path} (dry-run)")
        return

    os.makedirs(os.path.dirname(ini_path), exist_ok=True)
    fd, temp_path = tempfile.mkstemp(dir=os.path.dirname(ini_path), prefix="i_view.ini.")
    try:
        with os.fdopen(fd, "w", encoding="utf-8", newline="\n") as f:
            parser.write(f, space_around_delimiters=False)
        os.replace(temp_path, ini_path)
        log(f"Successfully wrote to {ini_path}")
    except Exception as e:
        os.remove(temp_path)
        raise e


def handle_request(request: dict) -> dict:
    req_id = request.get("requestId", "")
    cmd = request.get("command", "")

    if cmd == "check_installed":
        installed = check_installed()
        return {"requestId": req_id, "installed": installed}
    elif cmd == "apply":
        args = request.get("args", {})
        settings = args.get("settings", {})
        dry_run = args.get("dryRun", False)

        try:
            apply_settings(settings, dry_run)
            return {"requestId": req_id, "success": True}
        except Exception as e:
            return {"requestId": req_id, "error": str(e)}

    return {"requestId": req_id, "error": f"Unknown command: {cmd}"}


def main():
    if not sys.stdin.isatty():
        input_data = sys.stdin.read().strip()
        if not input_data:
            print(json.dumps({"error": "Empty input"}))
            sys.exit(1)

        try:
            request = json.loads(input_data)
        except json.JSONDecodeError as e:
            print(json.dumps({"error": f"Invalid JSON: {e}"}))
            sys.exit(1)

        response = handle_request(request)
        print(json.dumps(response))


if __name__ == "__main__":
    main()
