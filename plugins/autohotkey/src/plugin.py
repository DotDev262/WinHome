import json
import os
import re
import shutil
import sys
import tempfile

HEADER_LINE = "#Requires AutoHotkey v2.0"
SETTINGS_START = "; WinHome managed settings start"
SETTINGS_END = "; WinHome managed settings end"
HOTKEYS_START = "; WinHome managed hotkeys start"
HOTKEYS_END = "; WinHome managed hotkeys end"
HOTSTRINGS_START = "; WinHome managed hotstrings start"
HOTSTRINGS_END = "; WinHome managed hotstrings end"


def log(msg):
    sys.stderr.write(f"[autohotkey-plugin] {msg}\n")
    sys.stderr.flush()


def normalize_newlines(text: str) -> str:
    return text.replace("\r\n", "\n").replace("\r", "\n")


def read_text(file_path: str) -> str:
    if not os.path.exists(file_path):
        return ""

    with open(file_path, "r", encoding="utf-8") as handle:
        return handle.read()


def write_text(file_path: str, content: str) -> None:
    directory = os.path.dirname(file_path)
    os.makedirs(directory, exist_ok=True)

    temp_file = None
    try:
        with tempfile.NamedTemporaryFile(
            mode="w",
            encoding="utf-8",
            dir=directory,
            delete=False,
            newline="\n",
        ) as handle:
            temp_file = handle.name
            handle.write(content)

        os.replace(temp_file, file_path)
    except Exception:
        if temp_file and os.path.exists(temp_file):
            os.unlink(temp_file)
        raise


def find_ahk_executable() -> bool:
    if shutil.which("AutoHotkey64.exe") or shutil.which("AutoHotkey.exe"):
        return True

    search_roots = []

    program_files = os.getenv("PROGRAMFILES")
    if program_files:
        search_roots.append(os.path.join(program_files, "AutoHotkey", "v2"))

    program_files_x86 = os.getenv("PROGRAMFILES(X86)")
    if program_files_x86:
        search_roots.append(os.path.join(program_files_x86, "AutoHotkey", "v2"))

    local_app_data = os.getenv("LOCALAPPDATA")
    if local_app_data:
        search_roots.append(os.path.join(local_app_data, "Programs", "AutoHotkey"))

    executable_names = ["AutoHotkey64.exe", "AutoHotkey.exe"]

    for root in search_roots:
        if not root:
            continue

        for executable_name in executable_names:
            candidate = os.path.join(root, executable_name)
            if os.path.exists(candidate):
                return True

    return False


def check_installed(args: dict, request_id: str) -> dict:
    return {
        "requestId": request_id,
        "success": True,
        "changed": False,
        "data": find_ahk_executable(),
    }


def build_settings_block(settings: dict) -> list[str]:
    lines = [SETTINGS_START]

    if settings.get("persistent"):
        lines.append("Persistent")

    detect_hidden_windows = settings.get("detect_hidden_windows")
    if detect_hidden_windows:
        lines.append(f'DetectHiddenWindows "{detect_hidden_windows}"')

    icon_tip = settings.get("icon_tip")
    if icon_tip:
        lines.append(f'TrayTip "{icon_tip}"')

    lines.append(SETTINGS_END)
    return lines


def build_hotkey_block(trigger: str, action: str) -> list[str]:
    return [
        HOTKEYS_START,
        f"{trigger}::",
        "{",
        f"    {action}",
        "}",
        HOTKEYS_END,
    ]


def build_hotstring_lines(trigger: str, replacement: str) -> list[str]:
    normalized_replacement = replacement.replace("\r\n", "\n").replace("\r", "\n")

    if "\n" not in normalized_replacement:
        return [
            HOTSTRINGS_START,
            f"{trigger}{normalized_replacement}",
            HOTSTRINGS_END,
        ]

    replacement_lines = normalized_replacement.split("\n")
    return [
        HOTSTRINGS_START,
        f"{trigger}::",
        "(",
        *replacement_lines,
        ")",
        HOTSTRINGS_END,
    ]


def build_managed_content(args: dict) -> str:
    settings = args.get("settings", {})
    hotkeys = args.get("hotkeys", {})
    hotstrings = args.get("hotstrings", {})

    sections: list[str] = [HEADER_LINE]

    if settings:
        sections.extend([""] + build_settings_block(settings))

    if hotkeys:
        for trigger, action in hotkeys.items():
            sections.extend([""] + build_hotkey_block(trigger, action))

    if hotstrings:
        for trigger, replacement in hotstrings.items():
            sections.extend([""] + build_hotstring_lines(trigger, replacement))

    return "\n".join(sections).rstrip() + "\n"


def strip_managed_content(existing_text: str) -> str:
    if not existing_text:
        return ""

    lines = normalize_newlines(existing_text).split("\n")
    kept_lines: list[str] = []
    i = 0

    while i < len(lines):
        line = lines[i]
        stripped = line.strip()

        if stripped == HEADER_LINE:
            i += 1
            continue

        if stripped in {
            SETTINGS_START,
            SETTINGS_END,
            HOTKEYS_START,
            HOTKEYS_END,
            HOTSTRINGS_START,
            HOTSTRINGS_END,
        }:
            i += 1
            continue

        if stripped == "Persistent":
            i += 1
            continue

        if re.fullmatch(r'DetectHiddenWindows\s+".*"', stripped):
            i += 1
            continue

        if re.fullmatch(r'TrayTip\s+".*"', stripped):
            i += 1
            continue

        if stripped.endswith("::") and i + 1 < len(lines) and lines[i + 1].strip() == "{":
            i += 2
            depth = 1
            while i < len(lines) and depth > 0:
                current = lines[i].strip()
                if current == "{":
                    depth += 1
                elif current == "}":
                    depth -= 1
                i += 1
            continue

        if stripped.endswith("::") and i + 1 < len(lines) and lines[i + 1].strip() == "(":
            i += 2
            while i < len(lines) and lines[i].strip() != ")":
                i += 1
            if i < len(lines):
                i += 1
            continue

        if stripped.startswith("::") and "::" in stripped[2:]:
            i += 1
            continue

        kept_lines.append(line)
        i += 1

    while kept_lines and kept_lines[0] == "":
        kept_lines.pop(0)

    while kept_lines and kept_lines[-1] == "":
        kept_lines.pop()

    if not kept_lines:
        return ""

    return "\n".join(kept_lines) + "\n"


def apply_config(args: dict, context: dict, request_id: str) -> dict:
    dry_run = bool(context.get("dryRun", False))
    script_path = os.path.expandvars(args.get("script_path", ""))

    if not script_path:
        raise Exception("script_path is required")

    existing_text = read_text(script_path)
    custom_text = strip_managed_content(existing_text)
    managed_text = build_managed_content(args)
    merged_text = managed_text + ("\n" + custom_text if custom_text else "")
    merged_text = merged_text.rstrip() + "\n"

    if merged_text == existing_text:
        return {
            "requestId": request_id,
            "success": True,
            "changed": False,
        }

    if dry_run:
        log(f"Would update script: {script_path}")
        return {
            "requestId": request_id,
            "success": True,
            "changed": True,
        }

    write_text(script_path, merged_text)

    return {
        "requestId": request_id,
        "success": True,
        "changed": True,
    }


def process_request(request: dict) -> dict:
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
            return check_installed(args, request_id)

        if command == "apply":
            return apply_config(args, context, request_id)

        response["error"] = f"Unknown command: {command}"
    except Exception as fatal_err:
        response["error"] = f"Internal Script Error: {str(fatal_err)}"

    return response


def main():
    input_data = sys.stdin.read()

    if not input_data:
        return

    try:
        request = json.loads(input_data)
    except Exception as e:
        log(f"Failed to parse request: {e}")
        response = {
            "requestId": "unknown",
            "success": False,
            "changed": False,
            "error": f"Invalid JSON: {e}",
        }
        sys.stdout.write(json.dumps(response) + "\n")
        sys.stdout.flush()
        return

    response = process_request(request)

    sys.stdout.write(json.dumps(response) + "\n")
    sys.stdout.flush()


if __name__ == "__main__":
    main()
