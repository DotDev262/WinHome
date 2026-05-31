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
#
# audacity.cfg is INI-like but has a top-level [root] section for keys that
# appear before any section header, which configparser would reject.
# We normalise by inserting a synthetic [root] header when needed, then
# strip it back out on write so the file stays Audacity-compatible.
# ---------------------------------------------------------------------------

_SYNTHETIC_SECTION = "__root__"


def read_cfg(file_path: str) -> configparser.RawConfigParser:
    """Parse audacity.cfg into a RawConfigParser.

    Keys that appear before the first section header are placed under the
    synthetic section _SYNTHETIC_SECTION so configparser can handle them.
    """
    parser = configparser.RawConfigParser()
    parser.optionxform = str  # preserve key case

    if not os.path.exists(file_path):
        return parser

    with open(file_path, "r", encoding="utf-8") as f:
        raw = f.read()

    # If the file starts with content before the first '[', prepend a header.
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
    """Write a RawConfigParser back to an audacity.cfg file atomically.

    The synthetic root section header is omitted so the output matches
    Audacity's expected format.  All other section headers are preserved.
    Atomic: write to a temp file next to the target then os.replace().
    """
    os.makedirs(os.path.dirname(file_path), exist_ok=True)

    dir_name = os.path.dirname(file_path)
    fd, tmp_path = tempfile.mkstemp(dir=dir_name, suffix=".tmp")

    try:
        with os.fdopen(fd, "w", encoding="utf-8", newline="\n") as f:
            for section in parser.sections():
                if section == _SYNTHETIC_SECTION:
                    # Write bare key=value lines (no section header)
                    for key, value in parser.items(section):
                        f.write(f"{key}={value}\n")
                else:
                    f.write(f"[{section}]\n")
                    for key, value in parser.items(section):
                        f.write(f"{key}={value}\n")
                f.write("\n")

        os.replace(tmp_path, file_path)

    except Exception:
        # Clean up temp file on failure
        try:
            os.unlink(tmp_path)
        except OSError:
            pass
        raise


def _cast_value(value) -> str:
    """Convert a Python value to the string form Audacity stores."""
    if isinstance(value, bool):
        return "1" if value else "0"
    return str(value)


def merge_settings(
    parser: configparser.RawConfigParser,
    settings: dict,
) -> bool:
    """Merge a two-level settings dict (section/key) into the parser.

    Settings keys use the form ``"Section/Key"`` (e.g. ``"AudioIO/SampleRate"``).
    Returns True if any value was actually changed.
    """
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

def check_installed(args: dict, request_id: str) -> dict:
    """Return True if the Audacity config directory exists OR audacity.exe
    is on the PATH — whichever is found first."""
    app_dir = get_app_dir()

    if app_dir is None:
        return {
            "requestId": request_id,
            "success": False,
            "changed": False,
            "error": "APPDATA environment variable not found",
        }

    # Check 1: %APPDATA%\audacity\ directory exists
    if os.path.isdir(app_dir):
        return {
            "requestId": request_id,
            "success": True,
            "changed": False,
            "data": True,
        }

    # Check 2: audacity.exe present anywhere on PATH
    path_dirs = os.environ.get("PATH", "").split(os.pathsep)
    for directory in path_dirs:
        candidate = os.path.join(directory, "audacity.exe")
        if os.path.isfile(candidate):
            return {
                "requestId": request_id,
                "success": True,
                "changed": False,
                "data": True,
            }

    return {
        "requestId": request_id,
        "success": True,
        "changed": False,
        "data": False,
    }


def apply_config(args: dict, context: dict, request_id: str) -> dict:
    dry_run = context.get("dryRun", False)
    settings = args.get("settings", {})

    try:
        config_path = get_config_path()

        parser = read_cfg(config_path)

        changed = merge_settings(parser, settings)

        if not changed:
            return {
                "requestId": request_id,
                "success": True,
                "changed": False,
            }

        if dry_run:
            log(
                f"Would update {config_path} with: "
                f"{json.dumps(settings)}"
            )

            return {
                "requestId": request_id,
                "success": True,
                "changed": False,
            }

        write_cfg(config_path, parser)

        log(f"Updated Audacity config: {config_path}")

        return {
            "requestId": request_id,
            "success": True,
            "changed": True,
        }

    except Exception as e:
        log(f"Failed to apply config: {e}")

        return {
            "requestId": request_id,
            "success": False,
            "changed": False,
            "error": str(e),
        }


# ---------------------------------------------------------------------------
# Entry point
# ---------------------------------------------------------------------------

def main():
    input_data = sys.stdin.read()

    if not input_data:
        return

    try:
        request = json.loads(input_data)
    except Exception as e:
        log(f"Failed to parse request: {e}")
        sys.exit(1)

    request_id = request.get("requestId", "unknown")
    command = request.get("command")
    args = request.get("args", {})
    context = request.get("context", {})

    response = {
        "requestId": request_id,
        "success": False,
        "changed": False,
    }

    try:
        if command == "check_installed":
            response = check_installed(args, request_id)

        elif command == "apply":
            response = apply_config(args, context, request_id)

        else:
            response["error"] = f"Unknown command: {command}"

    except Exception as fatal_err:
        response["error"] = f"Internal Script Error: {str(fatal_err)}"

    sys.stdout.write(json.dumps(response) + "\n")
    sys.stdout.flush()


if __name__ == "__main__":
    main()

