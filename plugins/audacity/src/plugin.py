import configparser
import json
import os
import sys
import tempfile

CONFIG_FILENAME = "audacity.cfg"
APP_DIR_NAME = "audacity"


def log(msg):
    sys.stderr.write(f"[audacity-plugin] {msg}\n")
    sys.stderr.flush()


def get_config_path():
    appdata = os.getenv("APPDATA")

    if not appdata:
        raise Exception("APPDATA environment variable not found")

    config_dir = os.path.join(appdata, APP_DIR_NAME)
    os.makedirs(config_dir, exist_ok=True)

    return os.path.join(config_dir, CONFIG_FILENAME)


def get_app_dir():
    appdata = os.getenv("APPDATA")

    if not appdata:
        return None

    return os.path.join(appdata, APP_DIR_NAME)


# ---------------------------------------------------------------------------
# Audacity config parsing
# ---------------------------------------------------------------------------

_SYNTHETIC_SECTION = "__root__"


def read_cfg(file_path: str) -> configparser.RawConfigParser:
    parser = configparser.RawConfigParser()
    parser.optionxform = str  # preserve key case

    if not os.path.exists(file_path):
        return parser

    with open(file_path, "r", encoding="utf-8") as f:
        raw = f.read()

    lines = raw.splitlines(keepends=True)
    needs_synthetic = lines and not lines[0].lstrip().startswith("[")

    if needs_synthetic:
        raw = f"[{_SYNTHETIC_SECTION}]\n" + raw

    try:
        parser.read_string(raw)
    except Exception as e:
        log(f"Warning: could not fully parse {file_path}: {e}")

    return parser


def write_cfg(file_path: str, parser: configparser.RawConfigParser) -> None:
    os.makedirs(os.path.dirname(file_path), exist_ok=True)

    dir_name = os.path.dirname(file_path)
    fd, tmp_path = tempfile.mkstemp(dir=dir_name, suffix=".tmp")

    try:
        with os.fdopen(fd, "w", encoding="utf-8", newline="\n") as f:
            for section in parser.sections():
                if section == _SYNTHETIC_SECTION:
                    for key, value in parser.items(section):
                        f.write(f"{key}={value}\n")
                else:
                    f.write(f"[{section}]\n")
                    for key, value in parser.items(section):
                        f.write(f"{key}={value}\n")
                f.write("\n")

        os.replace(tmp_path, file_path)

    except Exception:
        try:
            os.unlink(tmp_path)
        except OSError:
            pass
        raise


def _cast_value(value) -> str:
    if isinstance(value, bool):
        return "1" if value else "0"
    return str(value)


def merge_settings(
    parser: configparser.RawConfigParser,
    settings: dict,
) -> bool:
    changed = False

    for dotted_key, value in settings.items():
        if "/" in dotted_key:
            section, key = dotted_key.split("/", 1)
        else:
            section = _SYNTHETIC_SECTION
            key = dotted_key

        str_value = _cast_value(value)

        if not parser.has_section(section):
            parser.add_section(section)
            parser.set(section, key, str_value)
            changed = True
        else:
            existing = parser.get(section, key) if parser.has_option(section, key) else None

            if existing != str_value:
                parser.set(section, key, str_value)
                changed = True

    return changed


# ---------------------------------------------------------------------------
# Plugin commands
# ---------------------------------------------------------------------------


# FIX 4: check_installed returns bare bool; main() wraps it
def check_installed(args: dict) -> bool:
    app_dir = get_app_dir()

    if app_dir is None:
        return False

    if os.path.isdir(app_dir):
        return True

    path_dirs = os.environ.get("PATH", "").split(os.pathsep)
    for directory in path_dirs:
        candidate = os.path.join(directory, "audacity.exe")
        if os.path.isfile(candidate):
            return True

    return False


def apply_config(args: dict, request_id: str) -> dict:
    # FIX 6: dryRun comes from args, not context
    dry_run = args.get("dryRun", False)
    settings = args.get("settings", {})

    try:
        config_path = get_config_path()

        parser = read_cfg(config_path)

        changed = merge_settings(parser, settings)

        if not changed:
            return {
                "requestId": request_id,
                "changed": False,
            }

        if dry_run:
            log(f"Would update {config_path} with: {json.dumps(settings)}")

            return {
                "requestId": request_id,
                "changed": True,
            }

        write_cfg(config_path, parser)

        log(f"Updated Audacity config: {config_path}")

        return {
            "requestId": request_id,
            "changed": True,
        }

    except Exception as e:
        log(f"Failed to apply config: {e}")

        return {
            "requestId": request_id,
            "error": str(e),
        }


# ---------------------------------------------------------------------------
# Entry point
# ---------------------------------------------------------------------------


def main():
    input_data = sys.stdin.read()

    # FIX 1: empty stdin returns JSON error response, never silent
    if not input_data:
        sys.stdout.write(
            json.dumps(
                {
                    "requestId": "unknown",
                    "error": "No input received",
                }
            )
            + "\n"
        )
        sys.stdout.flush()
        return

    # FIX 5: use plain dict key access so missing key gives None, not "unknown"
    request_id = None

    try:
        request = json.loads(input_data)
        # FIX 5: request.get("requestId") returns None when missing/null;
        # fall back to "unknown" only when truly absent
        request_id = request.get("requestId") or "unknown"
        command = request.get("command")
        args = request.get("args", {})
    except Exception as e:
        # FIX 2: JSON parse error returns JSON response, never sys.exit(1)
        log(f"Failed to parse request: {e}")
        sys.stdout.write(
            json.dumps(
                {
                    "requestId": request_id or "unknown",
                    "error": f"Failed to parse request: {e}",
                }
            )
            + "\n"
        )
        sys.stdout.flush()
        return

    try:
        if command == "check_installed":
            # FIX 4: wrap bare bool result into standard response envelope
            installed = check_installed(args)
            response = {
                "requestId": request_id,
                "installed": installed,
            }

        elif command == "apply":
            response = apply_config(args, request_id)

        else:
            response = {
                "requestId": request_id,
                "error": f"Unknown command: {command}",
            }

    except Exception as fatal_err:
        response = {
            "requestId": request_id,
            "error": f"Internal Script Error: {str(fatal_err)}",
        }

    sys.stdout.write(json.dumps(response) + "\n")
    sys.stdout.flush()


if __name__ == "__main__":
    main()
