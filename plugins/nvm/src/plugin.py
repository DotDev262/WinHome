import json
import os
import re
import shutil
import sys
import tempfile
from dataclasses import dataclass
from typing import Optional

SETTING_FILE = "settings.txt"
LINE_RE = re.compile(r"^(?P<leading>\s*)(?P<key>[^=\s#;]+)(?P<sep>\s*=\s*)(?P<value>.*)$")


def log(msg: str) -> None:
    sys.stderr.write(f"[nvm-plugin] {msg}\n")
    sys.stderr.flush()


def get_appdata_dir() -> str:
    appdata = os.environ.get("APPDATA")
    if appdata:
        return appdata

    userprofile = os.environ.get("USERPROFILE")
    if userprofile:
        return os.path.join(userprofile, "AppData", "Roaming")

    raise RuntimeError("Neither APPDATA nor USERPROFILE environment variable is set")


def get_settings_path() -> str:
    return os.path.join(get_appdata_dir(), "nvm", SETTING_FILE)


def normalize_value(value) -> str:
    if isinstance(value, bool):
        return "true" if value else "false"
    if value is None:
        return ""
    return str(value)


@dataclass
class SettingLine:
    raw: str
    key: Optional[str]
    value: Optional[str]
    leading: str = ""
    separator: str = ""
    suffix: str = ""


def split_value_and_suffix(raw_value: str) -> tuple[str, str]:
    in_single = False
    in_double = False

    for index, char in enumerate(raw_value):
        if char == "'" and not in_double:
            in_single = not in_single
            continue

        if char == '"' and not in_single:
            in_double = not in_double
            continue

        if in_single or in_double:
            continue

        if char in {"#", ";"} and index > 0 and raw_value[index - 1].isspace():
            start = index
            while start > 0 and raw_value[start - 1].isspace():
                start -= 1
            return raw_value[:start], raw_value[start:]

    return raw_value, ""


def parse_settings_text(text: str) -> list[SettingLine]:
    lines: list[SettingLine] = []

    for raw in text.splitlines(True):
        body = raw[:-1] if raw.endswith("\n") else raw
        stripped = body.lstrip()

        if not stripped or stripped.startswith("#") or stripped.startswith(";"):
            lines.append(SettingLine(raw=raw, key=None, value=None))
            continue

        match = LINE_RE.match(body)
        if not match:
            lines.append(SettingLine(raw=raw, key=None, value=None))
            continue

        value, suffix = split_value_and_suffix(match.group("value"))

        lines.append(
            SettingLine(
                raw=raw,
                key=match.group("key"),
                value=value,
                leading=match.group("leading"),
                separator=match.group("sep"),
                suffix=suffix,
            )
        )

    return lines


def read_settings_file(file_path: str) -> list[SettingLine]:
    if not os.path.exists(file_path):
        return []

    with open(file_path, "r", encoding="utf-8") as handle:
        return parse_settings_text(handle.read())


def render_lines(lines: list[SettingLine]) -> str:
    rendered: list[str] = []

    for line in lines:
        if line.key is None:
            rendered.append(line.raw)
            continue

        value = "" if line.value is None else line.value
        rendered.append(f"{line.leading}{line.key}{line.separator}{value}{line.suffix}\n")

    text = "".join(rendered)

    if text and not text.endswith("\n"):
        text += "\n"

    return text


def merge_settings(lines: list[SettingLine], settings: dict) -> bool:
    changed = False

    existing_keys: dict[str, list[int]] = {}
    for index, line in enumerate(lines):
        if line.key is not None:
            existing_keys.setdefault(line.key, []).append(index)

    for key, value in settings.items():
        if not isinstance(key, str):
            continue

        normalized_value = normalize_value(value)
        indices = existing_keys.get(key, [])

        if not indices:
            lines.append(
                SettingLine(
                    raw=f"{key}={normalized_value}\n",
                    key=key,
                    value=normalized_value,
                    leading="",
                    separator="=",
                    suffix="",
                )
            )
            changed = True
            continue

        for index in indices:
            current = lines[index]
            current_value = "" if current.value is None else current.value
            if current_value != normalized_value:
                lines[index] = SettingLine(
                    raw=(
                        f"{current.leading}{current.key}{current.separator}{normalized_value}{current.suffix}"
                        + ("\n" if current.raw.endswith("\n") else "")
                    ),
                    key=current.key,
                    value=normalized_value,
                    leading=current.leading,
                    separator=current.separator,
                    suffix=current.suffix,
                )
                changed = True

    return changed


def write_atomic(file_path: str, content: str) -> None:
    directory = os.path.dirname(file_path)
    os.makedirs(directory, exist_ok=True)

    fd, temp_path = tempfile.mkstemp(prefix="nvm-", dir=directory)
    try:
        with os.fdopen(fd, "w", encoding="utf-8", newline="\n") as handle:
            handle.write(content)
        os.replace(temp_path, file_path)
    except Exception:
        try:
            os.unlink(temp_path)
        except OSError:
            pass
        raise


def check_installed(_args: dict, request_id: str) -> dict:
    installed = (
        os.path.exists(get_settings_path()) or shutil.which("nvm.exe") is not None or shutil.which("nvm") is not None
    )

    return {
        "requestId": request_id,
        "success": True,
        "changed": False,
        "data": installed,
    }


def apply_config(args: dict, context: dict, request_id: str) -> dict:
    dry_run = bool(context.get("dryRun", False))
    settings = args.get("settings", {})

    if not isinstance(settings, dict):
        return {
            "requestId": request_id,
            "success": False,
            "changed": False,
            "error": "settings must be an object",
            "data": None,
        }

    try:
        settings_path = get_settings_path()
        lines = read_settings_file(settings_path)
        changed = merge_settings(lines, settings)

        data = {"path": settings_path, "settings": settings}

        if not changed:
            return {
                "requestId": request_id,
                "success": True,
                "changed": False,
                "data": data,
            }

        if dry_run:
            return {
                "requestId": request_id,
                "success": True,
                "changed": True,
                "data": data,
            }

        write_atomic(settings_path, render_lines(lines))

        return {
            "requestId": request_id,
            "success": True,
            "changed": True,
            "data": data,
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
        response = {
            "requestId": None,
            "success": False,
            "changed": False,
            "error": "Empty stdin",
            "data": None,
        }
        sys.stdout.write(json.dumps(response) + "\n")
        sys.stdout.flush()
        return

    try:
        request = json.loads(input_data)
    except Exception as exc:
        response = {
            "requestId": None,
            "success": False,
            "changed": False,
            "error": f"Failed to parse request: {exc}",
            "data": None,
        }
        sys.stdout.write(json.dumps(response) + "\n")
        sys.stdout.flush()
        return

    if not isinstance(request, dict):
        response = {
            "requestId": None,
            "success": False,
            "changed": False,
            "error": "Request must be a JSON object",
            "data": None,
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
            if not isinstance(args, dict):
                raise ValueError("args must be an object")
            if not isinstance(context, dict):
                raise ValueError("context must be an object")
            response = apply_config(args, context, request_id)
        else:
            response = {
                "requestId": request_id,
                "success": False,
                "changed": False,
                "error": f"Unknown command: {command}",
                "data": None,
            }
    except Exception as exc:
        response = {
            "requestId": request_id,
            "success": False,
            "changed": False,
            "error": f"Internal Script Error: {exc}",
            "data": None,
        }

    sys.stdout.write(json.dumps(response) + "\n")
    sys.stdout.flush()


if __name__ == "__main__":
    main()
