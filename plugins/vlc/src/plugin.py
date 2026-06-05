"""
VLC media player configuration provider plugin.

Manages settings in %APPDATA%\\vlc\\vlcrc using a custom INI-like parser
that preserves VLC's multi-value keys, unknown sections, and comments.

Protocol: JSON over stdin/stdout (WinHome plugin architecture).
"""

from __future__ import annotations

import json
import os
import sys
import tempfile
from pathlib import Path
from typing import Any


def log(msg: str) -> None:
    sys.stderr.write(f"[vlc-plugin] {msg}\n")
    sys.stderr.flush()


def _vlcrc_path() -> Path:
    appdata = os.environ.get("APPDATA", "")
    return Path(appdata) / "vlc" / "vlcrc"


def _find_vlc_exe() -> bool:
    for directory in os.environ.get("PATH", "").split(os.pathsep):
        candidate = Path(directory) / "vlc.exe"
        if candidate.is_file():
            return True
    return False


class _VlcrcDoc:
    def __init__(self) -> None:
        self._lines: list[str] = []

    def read(self, path: Path) -> None:
        if path.is_file():
            text = path.read_text(encoding="utf-8", errors="replace")
            self._lines = text.rstrip("\n").split("\n")
        else:
            self._lines = []

    def write(self, path: Path) -> None:
        path.parent.mkdir(parents=True, exist_ok=True)
        fd, tmp = tempfile.mkstemp(dir=path.parent, suffix=".tmp")
        try:
            with os.fdopen(fd, "w", encoding="utf-8", newline="\n") as fh:
                fh.write("\n".join(self._lines))
                fh.write("\n")
            os.replace(tmp, path)
        except Exception:
            try:
                os.unlink(tmp)
            except OSError:
                pass
            raise

    @staticmethod
    def _parse_kv(line: str):
        stripped = line.strip()
        if stripped.startswith("#") or stripped.startswith(";") or "=" not in stripped:
            return None
        key, _, value = stripped.partition("=")
        return key.strip(), value.strip()

    def _find_key_indices(self, key: str) -> list[int]:
        result = []
        for i, line in enumerate(self._lines):
            kv = self._parse_kv(line)
            if kv and kv[0] == key:
                result.append(i)
        return result

    def set(self, key: str, value: Any) -> None:
        str_value = _to_str(value)
        indices = self._find_key_indices(key)
        if indices:
            self._lines[indices[0]] = f"{key}={str_value}"
            for i in reversed(indices[1:]):
                del self._lines[i]
        else:
            self._lines.append(f"{key}={str_value}")

    def merge(self, settings: dict[str, Any]) -> None:
        for key, value in settings.items():
            self.set(key, value)


def _to_str(value: Any) -> str:
    if isinstance(value, bool):
        return "1" if value else "0"
    return str(value)


def check_installed(args: dict, request_id: str) -> dict:
    vlc_dir = _vlcrc_path().parent
    installed: bool = vlc_dir.is_dir() or _find_vlc_exe()
    return {
        "requestId": request_id,
        "success": True,
        "changed": False,
        "data": installed,
    }


def apply_config(args: dict, context: dict, request_id: str) -> dict:
    dry_run: bool = bool(context.get("dryRun", False))
    settings: dict[str, Any] = args.get("settings", {})

    try:
        vlcrc = _vlcrc_path()
        doc = _VlcrcDoc()
        doc.read(vlcrc)
        doc.merge(settings)

        if dry_run:
            log(f"dryRun: would write {len(settings)} key(s) to {vlcrc}")
            return {
                "requestId": request_id,
                "success": True,
                "changed": False,
            }

        doc.write(vlcrc)
        log(f"Updated vlcrc: {vlcrc}")
        return {
            "requestId": request_id,
            "success": True,
            "changed": bool(settings),
        }

    except Exception as exc:
        log(f"Failed to apply config: {exc}")
        return {
            "requestId": request_id,
            "success": False,
            "changed": False,
            "error": str(exc),
        }


def main() -> None:
    input_data = sys.stdin.read()
    if not input_data:
        return

    try:
        request = json.loads(input_data)
    except Exception as exc:
        log(f"Failed to parse request: {exc}")
        sys.exit(1)

    request_id = request.get("requestId", "unknown")
    command = request.get("command")
    args = request.get("args", {})
    context = request.get("context", {})

    response: dict = {
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
    except Exception as fatal:
        response["error"] = f"Internal Script Error: {fatal}"

    sys.stdout.write(json.dumps(response) + "\n")
    sys.stdout.flush()


if __name__ == "__main__":
    main()