"""Rainmeter plugin for WinHome.

This module implements a JSON-over-stdio protocol for two commands:
- check_installed: detect Rainmeter installation
- apply: merge provided settings into the Rainmeter.ini file

The implementation is modular to allow importing helpers from tests.
"""
from __future__ import annotations

import configparser
import json
import os
import shutil
import sys
import tempfile
from pathlib import Path
from typing import Dict, Any, List, Tuple, Optional


def create_response(request_id: str, data: Optional[Dict[str, Any]] = None, error: Optional[str] = None) -> Dict[str, Any]:
    """Create a standardized response including requestId."""
    resp: Dict[str, Any] = {"requestId": request_id}
    if error is not None:
        resp["error"] = error
    else:
        resp["data"] = data if data is not None else {}
    return resp


def find_rainmeter_executable() -> Optional[Path]:
    """Return the Path to Rainmeter.exe if found, otherwise None.

    Checks:
    - PATH via shutil.which for common executable names
    - Default install folder: C:\\Program Files\\Rainmeter\\Rainmeter.exe
    """
    # Try common names in PATH
    for name in ("Rainmeter.exe", "Rainmeter"):
        path = shutil.which(name)
        if path:
            return Path(path)

    # Check Program Files location
    program_files = os.environ.get("ProgramFiles", r"C:\Program Files")
    candidate = Path(program_files) / "Rainmeter" / "Rainmeter.exe"
    if candidate.exists():
        return candidate

    return None


def get_rainmeter_ini_path() -> Path:
    """Return the Path to the Rainmeter.ini file under %APPDATA%.

    Uses the APPDATA environment variable (Windows behavior).
    """
    appdata = os.environ.get("APPDATA")
    if not appdata:
        # Fall back to user home, but warn
        home = Path.home()
        appdata_path = home / "AppData" / "Roaming"
    else:
        appdata_path = Path(appdata)
    return appdata_path / "Rainmeter" / "Rainmeter.ini"


def read_config(path: Path) -> configparser.ConfigParser:
    """Read an INI file into a ConfigParser.

    Preserve case for option names and avoid interpolation.
    """
    config = configparser.ConfigParser(interpolation=None)
    config.optionxform = str  # preserve case
    if path.exists():
        config.read(path, encoding="utf-8")
    return config


def compute_changes(config: configparser.ConfigParser, new_settings: Dict[str, Dict[str, Any]]) -> List[Dict[str, Any]]:
    """Compute a list of changes that would be applied.

    Each change is a dict: {section, key, old, new}
    """
    changes: List[Dict[str, Any]] = []
    for section, options in new_settings.items():
        if not config.has_section(section):
            for key, new_val in options.items():
                changes.append({"section": section, "key": key, "old": None, "new": str(new_val)})
        else:
            for key, new_val in options.items():
                old = config.get(section, key) if config.has_option(section, key) else None
                if old != str(new_val):
                    changes.append({"section": section, "key": key, "old": old, "new": str(new_val)})
    return changes


def merge_settings(config: configparser.ConfigParser, new_settings: Dict[str, Dict[str, Any]]) -> List[Dict[str, Any]]:
    """Merge provided settings into the ConfigParser in-place.

    Returns the list of changes applied (same format as compute_changes).
    """
    changes = compute_changes(config, new_settings)

    for section, options in new_settings.items():
        if not config.has_section(section):
            config.add_section(section)
        for key, value in options.items():
            config.set(section, key, str(value))

    return changes


def atomic_write_config(config: configparser.ConfigParser, dest: Path) -> None:
    """Atomically write `config` to `dest` using a temp file and os.replace.

    Ensures parent directories exist.
    """
    dest.parent.mkdir(parents=True, exist_ok=True)
    fd, tmp_path = tempfile.mkstemp(prefix="rainmeter-", suffix=".ini", dir=str(dest.parent))
    try:
        with os.fdopen(fd, "w", encoding="utf-8") as tmp:
            config.write(tmp)
        # Use os.replace for atomic rename on Windows and POSIX
        os.replace(tmp_path, str(dest))
    except Exception:
        # Clean up tmp file if something went wrong
        try:
            Path(tmp_path).unlink(missing_ok=True)
        except Exception:
            pass
        raise


def handle_check_installed(request_id: str) -> Dict[str, Any]:
    """Handler for the `check_installed` command."""
    try:
        path = find_rainmeter_executable()
        installed = path is not None
        return create_response(request_id, {"installed": installed})
    except Exception as exc:
        return create_response(request_id, error=str(exc))


def handle_apply(request_id: str, args: Dict[str, Any]) -> Dict[str, Any]:
    """Handler for the `apply` command.

    Args expected in `args`:
    - dry_run: bool
    - settings: Dict[str, Dict[str, Any]]
    """
    try:
        dry_run = bool(args.get("dry_run", False))
        settings = args.get("settings", {}) or {}
        if not isinstance(settings, dict):
            raise ValueError("`settings` must be a mapping of sections to key/value pairs")

        ini_path = get_rainmeter_ini_path()
        config = read_config(ini_path)
        changes = merge_settings(config, settings)

        if dry_run:
            # Don't write, just show changes
            return create_response(request_id, {"dry_run": True, "changes": changes})

        # Write changes atomically
        atomic_write_config(config, ini_path)
        return create_response(request_id, {"dry_run": False, "changes": changes, "path": str(ini_path)})
    except Exception as exc:
        return create_response(request_id, error=str(exc))


def dispatch_message(message: Dict[str, Any]) -> Dict[str, Any]:
    """Dispatch a parsed JSON message and return a response dict."""
    request_id = str(message.get("requestId", "unknown"))
    command = message.get("command")
    args = message.get("args", {})

    if command == "check_installed":
        return handle_check_installed(request_id)
    if command == "apply":
        return handle_apply(request_id, args)

    return create_response(request_id, error=f"Unknown command: {command}")


def main() -> None:
    """Read a single JSON message from stdin and write a JSON response to stdout.

    This implements a simple JSON-over-stdio protocol for integration with WinHome.
    """
    try:
        raw = sys.stdin.read()
        if not raw:
            print(json.dumps(create_response("unknown", error="No input provided")))
            return
        message = json.loads(raw)
    except Exception as exc:
        print(json.dumps(create_response("unknown", error=f"Invalid JSON input: {exc}")))
        return

    response = dispatch_message(message)
    sys.stdout.write(json.dumps(response))
    sys.stdout.flush()


if __name__ == "__main__":
    main()
