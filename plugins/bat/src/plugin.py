import json
import os
import platform
import shutil
import sys
import tempfile
import uuid
from dataclasses import dataclass
from pathlib import Path
from typing import Any, Dict, List, Optional, Tuple

MANAGED_PREFIX = "--"


def log(msg: str) -> None:
    sys.stderr.write(f"[bat-plugin] {msg}\n")
    sys.stderr.flush()


def make_response(
    request_id: Optional[str],
    success: bool,
    changed: bool,
    data: Any,
    error: Optional[str] = None,
) -> Dict[str, Any]:
    payload: Dict[str, Any] = {
        "requestId": request_id,
        "success": success,
        "changed": changed,
        "data": data,
    }
    if error is not None:
        payload["error"] = error
    return payload


def get_config_path() -> Path:
    system = platform.system()

    if system == "Windows":
        appdata = os.environ.get("APPDATA")
        if not appdata:
            # Keep behavior explicit; host will return structured error.
            raise RuntimeError("APPDATA environment variable not set")
        return Path(appdata) / "bat" / "config"

    return Path.home() / ".config" / "bat" / "config"


def check_installed(_args: dict = None, _request_id: str = "") -> bool:
    # Required bare boolean only.
    installed = shutil.which("bat.exe") is not None or shutil.which("bat") is not None
    return installed


@dataclass
class ConfigLine:
    raw: str
    key: Optional[str]
    value: Optional[str]
    managed: bool


def _parse_setting_key(raw_key: str) -> str:
    # Normalize: "--theme" or "--theme=" should become "--theme"
    if not raw_key.startswith(MANAGED_PREFIX):
        raise ValueError("setting must start with --")
    # Remove any trailing '=' if present.
    return raw_key.rstrip("=")


def parse_line(line: str) -> ConfigLine:
    # Preserve everything we don't understand.
    stripped = line.lstrip()

    if not stripped:
        return ConfigLine(raw=line, key=None, value=None, managed=False)

    if stripped.startswith("#"):
        return ConfigLine(raw=line, key=None, value=None, managed=False)

    if not stripped.startswith(MANAGED_PREFIX):
        return ConfigLine(raw=line, key=None, value=None, managed=False)

    # If a line starts like a managed setting but is malformed (e.g. "--theme" with no
    # '=' and no value), treat it as unknown/raw and never fail parsing.
    if "=" not in stripped and len(stripped.split()) == 1:
        return ConfigLine(raw=line, key=None, value=None, managed=False)

    # Accept formats:
    #   --key=value
    #   --key="value"
    #   --key=value
    #   --key value   (best-effort)
    remainder = stripped[len(MANAGED_PREFIX) :]
    if not remainder:
        return ConfigLine(raw=line, key=None, value=None, managed=False)

    # Split on first '=' if present.
    if "=" in remainder:
        raw_key, raw_value = remainder.split("=", 1)
        key = "--" + _parse_setting_key(raw_key)
        value = raw_value
        # Remove a single pair of surrounding double quotes.
        if len(value) >= 2 and value[0] == '"' and value[-1] == '"':
            value = value[1:-1]
        return ConfigLine(raw=line, key=key, value=value, managed=True)

    # Best-effort support for space-delimited: --key value
    parts = remainder.split(None, 1)
    if len(parts) == 2:
        raw_key, raw_value = parts
        key = "--" + _parse_setting_key(raw_key)
        value = raw_value.strip()
        if len(value) >= 2 and value[0] == '"' and value[-1] == '"':
            value = value[1:-1]
        return ConfigLine(raw=line, key=key, value=value, managed=True)

    return ConfigLine(raw=line, key=None, value=None, managed=False)


def parse_config(text: str) -> Tuple[List[ConfigLine], bool]:
    """Returns (lines, corrupted)."""
    # This parser is line-based and should be resilient.
    # Mark corrupted only for situations we truly cannot proceed with.
    try:
        # Re-parse with correct raw retention: splitlines(True) keeps newlines.
        fixed: List[ConfigLine] = []
        for original in text.splitlines(True):
            # Keep original raw including newline.
            no_newline = original[:-1] if original.endswith("\n") else original
            parsed = parse_line(no_newline)
            parsed.raw = original
            fixed.append(parsed)
        return fixed, False
    except Exception as exc:
        log(f"Warning: failed to parse bat config: {exc}")
        return [], True


def stringify_value(value: Any) -> str:
    if value is None:
        return ""
    if isinstance(value, bool):
        return "true" if value else "false"
    return str(value)


def build_setting_line(key: str, value: Any) -> str:
    # bat config is line-based. Prefer --key=value.
    # Always escape double quotes inside value.
    s = stringify_value(value)
    s = s.replace('"', '\\"')
    return f"{key}={s}"


def merge_settings(lines: List[ConfigLine], settings: Dict[str, Any]) -> Tuple[List[ConfigLine], bool]:
    # Normalize incoming keys to ensure they start with --
    normalized: Dict[str, Any] = {}
    for k, v in settings.items():
        if not isinstance(k, str):
            continue
        if not k.startswith(MANAGED_PREFIX):
            normalized[MANAGED_PREFIX + k] = v
        else:
            normalized[k] = v

    settings = normalized
    changed = False

    # Map existing managed keys to their indices.
    existing_indices: Dict[str, int] = {}
    for i, cl in enumerate(lines):
        if cl.managed and cl.key is not None:
            existing_indices[cl.key] = i

    # Update existing lines
    for key, value in settings.items():
        new_line = build_setting_line(key, value)
        if key in existing_indices:
            idx = existing_indices[key]
            # Preserve newline char from existing raw.
            existing_raw = lines[idx].raw
            newline = "\n" if existing_raw.endswith("\n") else ""
            desired_raw = new_line + newline
            if existing_raw != desired_raw:
                lines[idx] = ConfigLine(
                    raw=desired_raw,
                    key=key,
                    value=stringify_value(value),
                    managed=True,
                )
                changed = True
        else:
            # Append new managed setting at end.
            # Ensure we add a newline if file doesn't end with one.
            insertion = new_line
            if not lines:
                lines = [ConfigLine(raw=insertion + "\n", key=key, value=stringify_value(value), managed=True)]
                changed = True
                continue
            last_raw = lines[-1].raw
            if last_raw.endswith("\n"):
                insertion = insertion + "\n"
            else:
                insertion = "\n" + insertion + "\n"
            lines.append(
                ConfigLine(
                    raw=insertion,
                    key=key,
                    value=stringify_value(value),
                    managed=True,
                )
            )
            changed = True

    # Normalize any managed line with malformed content? Keep unknown/unmanaged untouched.
    # NOTE: merge_settings does not mark corruption; corruption is handled at file-load time.
    return lines, changed


def backup_corrupt_config(config_path: Path) -> Path:
    backup_path = config_path.parent / f"{config_path.name}.corrupt.{uuid.uuid4()}"
    shutil.copy2(str(config_path), str(backup_path))
    return backup_path


def write_atomic(target_path: Path, content: str) -> None:
    parent = target_path.parent
    parent.mkdir(parents=True, exist_ok=True)

    fd, temp_path = tempfile.mkstemp(
        prefix=f".{target_path.name}.",
        suffix=".tmp",
        dir=str(parent),
        text=True,
    )
    try:
        with os.fdopen(fd, "w", encoding="utf-8", newline="\n") as f:
            f.write(content)
        os.replace(temp_path, str(target_path))
    except Exception:
        try:
            if os.path.exists(temp_path):
                os.remove(temp_path)
        except Exception:
            pass
        raise


def load_config(config_path: Path) -> Tuple[List[ConfigLine], bool, str]:
    if not config_path.exists():
        return [], False, ""

    try:
        raw = config_path.read_text(encoding="utf-8")
    except UnicodeError:
        # Treat as corrupted/unparseable.
        return [], True, config_path.read_text(encoding="utf-8", errors="ignore")

    lines, corrupted = parse_config(raw)
    return lines, corrupted, raw


def format_lines(lines: List[ConfigLine]) -> str:
    # raw already contains newlines as appropriate.
    return "".join(cl.raw for cl in lines)


def handle_check_installed(request: dict) -> bool:
    return check_installed(request.get("args", {}), request.get("requestId", ""))


def handle_get(request: dict) -> Dict[str, Any]:
    request_id = request.get("requestId")
    try:
        config_path = get_config_path()
        lines, corrupted, _raw = load_config(config_path)

        settings: Dict[str, Any] = {}
        for cl in lines:
            if cl.managed and cl.key is not None and cl.value is not None:
                settings[cl.key] = cl.value

        data: Dict[str, Any] = {"settings": settings}
        if corrupted:
            data["corrupted"] = True
        return make_response(request_id, True, False, data)
    except Exception as exc:
        return make_response(request_id, False, False, {}, str(exc))


def handle_set(request: dict) -> Dict[str, Any]:
    request_id = request.get("requestId")
    context = request.get("context", {})
    dry_run = context.get("dryRun", False)

    try:
        settings = request.get("args", {}).get("settings", {})
        if not isinstance(settings, dict):
            raise ValueError("args.settings must be an object")

        config_path = get_config_path()
        lines, corrupted, _raw = load_config(config_path)

        merged_lines, changed = merge_settings(lines, settings)
        proposed = format_lines(merged_lines)

        data: Dict[str, Any] = {
            "path": str(config_path),
            "settings": settings,
            "corrupted": corrupted,
        }

        if not changed:
            return make_response(request_id, True, False, data)

        if dry_run:
            data["proposed"] = proposed
            return make_response(request_id, True, True, data)

        backup_path: Optional[Path] = None
        if corrupted and config_path.exists():
            backup_path = backup_corrupt_config(config_path)
            data["backupPath"] = str(backup_path)

        write_atomic(config_path, proposed)
        return make_response(request_id, True, True, data | ({"backupPath": str(backup_path)} if backup_path else {}))

    except Exception as exc:
        return make_response(request_id, False, False, {}, str(exc))


def dispatch(request: dict) -> Any:
    command = request.get("command")

    if command == "check_installed":
        # Must be bare boolean.
        return handle_check_installed(request)

    if command == "get":
        return handle_get(request)

    if command == "set":
        return handle_set(request)

    return make_response(request.get("requestId"), False, False, {}, f"Unknown command: {command}")


def main() -> None:
    raw = sys.stdin.read()

    if raw == "":
        sys.stdout.write(
            json.dumps(
                make_response(
                    None,
                    False,
                    False,
                    {"error": "Empty stdin"},
                    None,
                )
            )
            + "\n"
        )
        sys.stdout.flush()
        return

    try:
        request = json.loads(raw)
    except Exception as exc:
        result = make_response(None, False, False, {}, f"Failed to parse request: {exc}")
        sys.stdout.write(json.dumps(result) + "\n")
        sys.stdout.flush()
        return

    try:
        if not isinstance(request, dict):
            raise ValueError("Request must be a JSON object")
        result = dispatch(request)
    except Exception as exc:
        result = make_response(
            request.get("requestId", "unknown") if isinstance(request, dict) else "unknown",
            False,
            False,
            {},
            f"Internal Script Error: {exc}",
        )

    # check_installed expects bare bool, not JSON.
    if isinstance(result, bool):
        sys.stdout.write("true\n" if result else "false\n")
    else:
        sys.stdout.write(json.dumps(result) + "\n")

    sys.stdout.flush()


if __name__ == "__main__":
    main()
