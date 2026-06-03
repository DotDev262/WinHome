import json
import os
import shutil
import sys
from pathlib import Path

MARKER_START = "# --- WinHome managed start ---"
MARKER_END = "# --- WinHome managed end ---"


def log(msg):
    sys.stderr.write(f"[powershell-plugin] {msg}\n")
    sys.stderr.flush()


def get_profile_paths():
    paths = []

    user_profile = os.getenv("USERPROFILE")
    if user_profile:
        ps7_dir = os.path.join(user_profile, "Documents", "PowerShell")
        ps5_dir = os.path.join(user_profile, "Documents", "WindowsPowerShell")

        if os.path.exists(ps7_dir):
            paths.append(os.path.join(ps7_dir, "Microsoft.PowerShell_profile.ps1"))
        if os.path.exists(ps5_dir):
            paths.append(os.path.join(ps5_dir, "Microsoft.PowerShell_profile.ps1"))

        if paths:
            return paths

    # Fallback via Path.home()
    if os.name == "nt":
        fallback_dir = os.path.join(str(Path.home()), "Documents", "PowerShell")
        os.makedirs(fallback_dir, exist_ok=True)
        paths.append(os.path.join(fallback_dir, "Microsoft.PowerShell_profile.ps1"))
    else:
        fallback_dir = os.path.join(str(Path.home()), ".config", "powershell")
        os.makedirs(fallback_dir, exist_ok=True)
        paths.append(os.path.join(fallback_dir, "profile.ps1"))

    return paths


def read_profile(file_path: str):
    if not os.path.exists(file_path):
        return "", ""
    try:
        with open(file_path, "r", encoding="utf-8") as f:
            content = f.read()

        # Split by markers
        if MARKER_START in content and MARKER_END in content:
            parts = content.split(MARKER_START)
            before = parts[0]
            after = parts[1].split(MARKER_END)[1] if len(parts[1].split(MARKER_END)) > 1 else ""
            return before, after
        return content, ""
    except Exception as e:
        log(f"Warning: could not read {file_path}: {e}")
        return "", ""


def generate_script(settings: dict) -> str:
    lines = [MARKER_START]

    aliases = settings.get("aliases", {})
    if aliases:
        for k, v in aliases.items():
            k_esc = k.replace("'", "''")
            v_esc = v.replace("'", "''")
            if " " in v:
                lines.append(f"function {k} {{ {v} @args }}")
            else:
                lines.append(f"Set-Alias -Name '{k_esc}' -Value '{v_esc}' -Force")

    modules = settings.get("modules", {})
    if modules:
        for k, v in modules.items():
            lines.append(f"Import-Module -Name '{k}' -ErrorAction SilentlyContinue")
            if "init" in v:
                cmd = v["init"].get("cmd", "")
                hook = v["init"].get("hook", "")
                if cmd and hook:
                    lines.append(f"Invoke-Expression (& {k} init powershell --cmd {cmd} --hook {hook} | Out-String)")
                else:
                    lines.append(f"Invoke-Expression (& {k} init powershell | Out-String)")

    prompt = settings.get("prompt", {})
    if prompt:
        p_type = prompt.get("type")
        if p_type == "oh-my-posh":
            theme = prompt.get("theme", "")
            if theme:
                lines.append(f"oh-my-posh init powershell --config '{theme}' | Invoke-Expression")
            else:
                lines.append("oh-my-posh init powershell | Invoke-Expression")

    psreadline = settings.get("psreadline", {})
    if psreadline:
        lines.append("Import-Module -Name PSReadLine -ErrorAction SilentlyContinue")
        for k, v in psreadline.items():
            k_camel = "".join(word.capitalize() for word in k.split("_"))
            v_esc = str(v).replace("'", "''")
            lines.append(f"Set-PSReadLineOption -{k_camel} '{v_esc}'")

    functions = settings.get("functions", {})
    if functions:
        for k, v in functions.items():
            lines.append(f"function {k} {{\n    {v}\n}}")

    lines.append(MARKER_END)
    return "\n".join(lines)


def write_profile(file_path: str, content: str) -> None:
    os.makedirs(os.path.dirname(file_path), exist_ok=True)
    tmp = file_path + ".tmp"
    with open(tmp, "w", encoding="utf-8") as f:
        f.write(content)
    os.replace(tmp, file_path)


def check_installed(args: dict, request_id: str) -> dict:
    installed = (
        shutil.which("pwsh.exe") is not None
        or shutil.which("powershell.exe") is not None
        or shutil.which("pwsh") is not None
    )
    return {
        "requestId": request_id,
        "success": True,
        "changed": False,
        "data": {"installed": installed},
    }


def apply_config(args: dict, context: dict, request_id: str) -> dict:
    dry_run = context.get("dryRun", False)
    settings = args.get("settings", {})

    # Handle the specific apply input format from issue desc if necessary
    if "aliases" in args and "settings" not in args:
        settings = args

    try:
        paths = get_profile_paths()
        new_script = generate_script(settings)

        changed_any = False

        for p in paths:
            before, after = read_profile(p)
            new_content = before + new_script + after

            # Simple check to see if we'd actually change anything
            current_content = ""
            if os.path.exists(p):
                with open(p, "r", encoding="utf-8") as f:
                    current_content = f.read()

            if current_content != new_content:
                changed_any = True
                if not dry_run:
                    write_profile(p, new_content)
                    log(f"Updated powershell profile: {p}")
                else:
                    log(f"Would update {p} with new script block")

        if not changed_any:
            return {
                "requestId": request_id,
                "success": True,
                "changed": False,
            }

        return {
            "requestId": request_id,
            "success": True,
            "changed": changed_any,
        }

    except Exception as e:
        log(f"Failed to apply config: {e}")
        return {
            "requestId": request_id,
            "success": False,
            "changed": False,
            "error": str(e),
        }


def main():
    input_data = sys.stdin.read()
    if not input_data:
        return

    request_id = "unknown"
    try:
        request = json.loads(input_data)
        request_id = request.get("requestId", "unknown")
    except Exception as e:
        log(f"Failed to parse request: {e}")
        sys.stdout.write(
            json.dumps(
                {
                    "requestId": request_id,
                    "success": False,
                    "changed": False,
                    "error": f"Failed to parse request: {e}",
                }
            )
            + "\n"
        )
        sys.stdout.flush()
        return

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
