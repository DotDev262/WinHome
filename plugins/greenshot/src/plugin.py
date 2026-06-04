"""
Greenshot configuration provider plugin.
"""

from __future__ import annotations

import configparser
import json
import os
import sys
import tempfile
from pathlib import Path
from typing import Any


def _config_path() -> Path:
    appdata = os.environ.get("APPDATA", "")
    return Path(appdata) / "Greenshot" / "Greenshot.ini"


def _greenshot_appdata_dir() -> Path:
    return _config_path().parent


def _find_exe_in_path(name: str) -> bool:
    path_env = os.environ.get("PATH", "")
    for directory in path_env.split(os.pathsep):
        candidate = Path(directory) / name
        if candidate.is_file():
            return True
    return False


def _read_ini(path: Path) -> configparser.RawConfigParser:
    parser = configparser.RawConfigParser()
    parser.optionxform = str
    if path.is_file():
        parser.read(str(path), encoding="utf-8")
    return parser


def _deep_merge(parser: configparser.RawConfigParser, settings: dict[str, Any]) -> configparser.RawConfigParser:
    for section, keys in settings.items():
        if not isinstance(keys, dict):
            continue
        if not parser.has_section(section):
            parser.add_section(section)
        for key, value in keys.items():
            parser.set(section, key, _coerce_value(value))
    return parser


def _coerce_value(value: Any) -> str:
    if isinstance(value, bool):
        return "True" if value else "False"
    return str(value)


def _write_ini_atomic(parser: configparser.RawConfigParser, path: Path) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    fd, tmp_path = tempfile.mkstemp(dir=str(path.parent), prefix=".greenshot_tmp_", suffix=".ini")
    try:
        with os.fdopen(fd, "w", encoding="utf-8", newline="\n") as fh:
            parser.write(fh)
        os.replace(tmp_path, str(path))
    except Exception:
        try:
            os.unlink(tmp_path)
        except OSError:
            pass
        raise


def _ini_to_dict(parser: configparser.RawConfigParser) -> dict[str, dict[str, str]]:
    result: dict[str, dict[str, str]] = {}
    for section in parser.sections():
        result[section] = dict(parser.items(section))
    return result


def check_installed(args: dict[str, Any]) -> dict[str, Any]:
    request_id = args.get("requestId", "")
    appdata_present = _greenshot_appdata_dir().is_dir()
    exe_present = _find_exe_in_path("Greenshot.exe")
    installed = appdata_present or exe_present
    return {
        "requestId": request_id,
        "installed": installed,
        "details": {
            "appdataDir": str(_greenshot_appdata_dir()),
            "appdataDirExists": appdata_present,
            "exeOnPath": exe_present,
        },
    }


def apply(args: dict[str, Any]) -> dict[str, Any]:
    request_id = args.get("requestId", "")
    settings: dict[str, Any] = args.get("settings", {})
    dry_run: bool = bool(args.get("dryRun", False))
    config_path = _config_path()

    parser = _read_ini(config_path)
    before = _ini_to_dict(parser)
    parser = _deep_merge(parser, settings)
    after = _ini_to_dict(parser)

    changes: list[dict[str, Any]] = []
    for section, keys in after.items():
        for key, new_val in keys.items():
            old_val = before.get(section, {}).get(key)
            if old_val != new_val:
                changes.append({"section": section, "key": key, "old": old_val, "new": new_val})

    if not dry_run:
        _write_ini_atomic(parser, config_path)

    return {
        "requestId": request_id,
        "dryRun": dry_run,
        "configPath": str(config_path),
        "changes": changes,
        "changeCount": len(changes),
    }


def main() -> None:
    raw = sys.stdin.read()
    args: dict[str, Any] = json.loads(raw) if raw.strip() else {}
    action = args.get("action", "")

    if action == "check_installed":
        result = check_installed(args)
    elif action == "apply":
        result = apply(args)
    else:
        result = {
            "requestId": args.get("requestId", ""),
            "error": f"Unknown action: {action!r}",
            "supportedActions": ["check_installed", "apply"],
        }

    print(json.dumps(result, indent=2))


if __name__ == "__main__":
    main()
