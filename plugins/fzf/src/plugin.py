import json
import os
import shlex
import shutil
import sys
import tempfile
import uuid
from pathlib import Path
from typing import Any, Dict, Tuple

SUPPORTED_OPTION_SETTINGS = {
    "height",
    "border",
    "preview",
    "color",
    "bind",
    "layout",
}


def log(msg: str) -> None:
    sys.stderr.write(f"[fzf-plugin] {msg}\n")
    sys.stderr.flush()


def response(
    request_id: str,
    success: bool,
    changed: bool,
    data: Any,
    error: str | None = None,
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


def get_fzfrc_path() -> str:
    if os.name == "nt":
        user_profile = os.getenv("USERPROFILE")
        if not user_profile:
            raise RuntimeError("USERPROFILE environment variable not set")
        return os.path.join(user_profile, "_fzfrc")
    return str(Path.home() / ".fzfrc")


def check_installed(args: Dict[str, Any], request_id: str) -> Dict[str, Any]:
    executable = "fzf.exe" if os.name == "nt" else "fzf"
    installed = shutil.which(executable) is not None
    return response(request_id, True, False, installed)


def stringify_value(value: Any) -> str:
    if isinstance(value, bool):
        return "true" if value else "false"
    return str(value)


def parse_export_line(line: str) -> Tuple[str, str]:
    if not line.startswith("export "):
        raise ValueError("expected export KEY=value")

    assignment = line[len("export ") :].strip()
    if "=" not in assignment:
        raise ValueError("expected export KEY=value")

    key, raw_value = assignment.split("=", 1)
    if not key or not key.replace("_", "A").isalnum() or key[0].isdigit():
        raise ValueError(f"invalid export key: {key}")
    return key, parse_shell_value(raw_value.strip())


def parse_shell_value(raw_value: str) -> str:
    if not raw_value:
        return ""

    if raw_value[0] == "'":
        end = raw_value.find("'", 1)
        if end == -1:
            raise ValueError("unterminated single-quoted value")
        if raw_value[end + 1 :].strip():
            raise ValueError("unexpected text after quoted value")
        return raw_value[1:end]

    if raw_value[0] != '"':
        try:
            parts = shlex.split(raw_value, posix=True)
        except ValueError as exc:
            raise ValueError(f"invalid shell syntax: {exc}") from exc
        if len(parts) != 1:
            raise ValueError("expected a single shell value")
        return parts[0]

    value = []
    escaped = False
    for index, char in enumerate(raw_value[1:], start=1):
        if escaped:
            if char in {"\\", '"', "$", "`"}:
                value.append(char)
            else:
                value.append("\\")
                value.append(char)
            escaped = False
            continue
        if char == "\\":
            escaped = True
            continue
        if char == '"':
            if raw_value[index + 1 :].strip():
                raise ValueError("unexpected text after quoted value")
            return "".join(value)
        value.append(char)

    raise ValueError("unterminated double-quoted value")


def read_fzfrc(file_path: str) -> Tuple[Dict[str, str], bool]:
    config: Dict[str, str] = {}
    if not os.path.exists(file_path):
        return config, False

    try:
        with open(file_path, "r", encoding="utf-8") as f:
            for line_number, raw_line in enumerate(f, start=1):
                line = raw_line.strip()
                if not line or line.startswith("#"):
                    continue
                if not line.startswith("export "):
                    raise ValueError(
                        f"line {line_number}: expected export statement"
                    )
                key, value = parse_export_line(line)
                config[key] = value
    except (OSError, UnicodeError, ValueError) as exc:
        log(
            f"Warning: failed to parse existing config ({exc}). Backing up and starting fresh."
        )
        return {}, True

    return config, False


def shell_quote(value: str) -> str:
    if "\n" in value or "\r" in value:
        raise ValueError("fzf config values cannot contain newlines")
    escaped = (
        value.replace("\\", "\\\\")
        .replace('"', '\\"')
        .replace("$", "\\$")
        .replace("`", "\\`")
    )
    return f'"{escaped}"'


def write_fzfrc(file_path: str, config: Dict[str, str]) -> None:
    parent = os.path.dirname(file_path)
    if parent:
        os.makedirs(parent, exist_ok=True)

    fd, temp_path = tempfile.mkstemp(
        prefix=".fzfrc.", suffix=".tmp", dir=parent or None, text=True
    )
    try:
        with os.fdopen(fd, "w", encoding="utf-8", newline="\n") as f:
            for key in sorted(config):
                f.write(f"export {key}={shell_quote(config[key])}\n")
        os.replace(temp_path, file_path)
    except Exception:
        try:
            os.remove(temp_path)
        except OSError:
            pass
        raise


def backup_corrupted_config(file_path: str) -> str:
    backup_path = f"{file_path}.bak.{uuid.uuid4()}"
    shutil.copy2(file_path, backup_path)
    return backup_path


def build_default_opts(
    settings: Dict[str, Any], existing_value: str | None
) -> str | None:
    explicit = settings.get("FZF_DEFAULT_OPTS")
    if explicit is not None:
        opts = stringify_value(explicit).strip()
    else:
        opts = (existing_value or "").strip()

    generated = []
    for key in sorted(SUPPORTED_OPTION_SETTINGS):
        if key not in settings:
            continue
        value = stringify_value(settings[key])
        generated.append(f"--{key} {value}")

    if generated:
        opts = " ".join(part for part in [opts, *generated] if part).strip()
    return opts or None


def merge_config(target: Dict[str, str], settings: Dict[str, Any]) -> bool:
    changed = False

    default_opts = build_default_opts(settings, target.get("FZF_DEFAULT_OPTS"))
    if (
        default_opts is not None
        and target.get("FZF_DEFAULT_OPTS") != default_opts
    ):
        target["FZF_DEFAULT_OPTS"] = default_opts
        changed = True

    for key, value in settings.items():
        if key in SUPPORTED_OPTION_SETTINGS or key == "FZF_DEFAULT_OPTS":
            continue
        if not key.startswith("FZF_"):
            continue
        string_value = stringify_value(value)
        if target.get(key) != string_value:
            target[key] = string_value
            changed = True

    return changed


def apply_config(
    args: Dict[str, Any], context: Dict[str, Any], request_id: str
) -> Dict[str, Any]:
    dry_run = context.get("dryRun", False) is True

    try:
        fzfrc_path = get_fzfrc_path()
        settings = args.get("settings", {})
        if not isinstance(settings, dict):
            raise ValueError("args.settings must be an object")

        current, corrupted = read_fzfrc(fzfrc_path)
        changed = corrupted
        changed = merge_config(current, settings) or changed

        data: Dict[str, Any] = {
            "path": fzfrc_path,
            "settings": current,
            "corrupted": corrupted,
        }

        if not changed:
            return response(request_id, True, False, data)

        if dry_run:
            log(f"Would update {fzfrc_path} with: {json.dumps(settings)}")
            if corrupted:
                log(f"Would back up corrupted config: {fzfrc_path}")
            return response(request_id, True, True, data)

        if corrupted and os.path.exists(fzfrc_path):
            backup_path = backup_corrupted_config(fzfrc_path)
            data["backupPath"] = backup_path
            log(f"Backed up corrupted fzf config: {backup_path}")

        write_fzfrc(fzfrc_path, current)
        log(f"Updated fzf config: {fzfrc_path}")
        return response(request_id, True, True, data)

    except Exception as exc:
        log(f"Failed to apply config: {exc}")
        return response(request_id, False, False, {}, str(exc))


def dispatch(request: Dict[str, Any]) -> Dict[str, Any]:
    request_id = request.get("requestId", "unknown")
    command = request.get("command")
    args = request.get("args", {})
    context = request.get("context", {})

    if not isinstance(args, dict):
        return response(request_id, False, False, {}, "args must be an object")
    if not isinstance(context, dict):
        return response(
            request_id, False, False, {}, "context must be an object"
        )

    try:
        if command == "check_installed":
            return check_installed(args, request_id)
        if command == "apply":
            return apply_config(args, context, request_id)
        return response(
            request_id, False, False, {}, f"Unknown command: {command}"
        )
    except Exception as exc:
        return response(
            request_id, False, False, {}, f"Internal Script Error: {exc}"
        )


def main() -> None:
    input_data = sys.stdin.read()
    if not input_data:
        sys.stdout.write(
            json.dumps(response("unknown", False, False, {}, "Empty stdin"))
            + "\n"
        )
        sys.stdout.flush()
        return

    try:
        request = json.loads(input_data)
    except Exception as exc:
        log(f"Failed to parse request: {exc}")
        sys.stdout.write(
            json.dumps(
                response(
                    "unknown",
                    False,
                    False,
                    {},
                    f"Failed to parse request: {exc}",
                )
            )
            + "\n"
        )
        sys.stdout.flush()
        return

    if not isinstance(request, dict):
        payload = response(
            "unknown", False, False, {}, "Request must be a JSON object"
        )
    else:
        payload = dispatch(request)

    sys.stdout.write(json.dumps(payload) + "\n")
    sys.stdout.flush()


if __name__ == "__main__":
    main()
