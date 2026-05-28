import json
import os
import shutil
import subprocess
import sys
from pathlib import Path

ENV_VARS = (
    "_ZO_MAX_DEPTH",
    "_ZO_ECHO",
    "_ZO_EXCLUDE_DIRS",
    "_ZO_RESOLVE_SYMLINKS",
)


def log(message: str) -> None:
    sys.stderr.write(f"[zoxide-plugin] {message}\n")
    sys.stderr.flush()


def get_user_home() -> Path:
    return Path(os.environ.get("USERPROFILE") or str(Path.home()))


def get_powershell_profile_path() -> Path:
    home = get_user_home()
    if sys.platform == "win32":
        return (
            home
            / "Documents"
            / "PowerShell"
            / "Microsoft.PowerShell_profile.ps1"
        )
    return home / ".config" / "powershell" / "profile.ps1"


def get_bash_profile_path() -> Path:
    home = get_user_home()
    bashrc = home / ".bashrc"
    bash_profile = home / ".bash_profile"

    if bashrc.exists():
        return bashrc
    if bash_profile.exists():
        return bash_profile
    return bashrc


def build_init_line(shell: str, init_args: dict) -> str:
    flags = []

    cmd = init_args.get("cmd")
    hook = init_args.get("hook")
    no_cmd = init_args.get("no_cmd", False)

    if cmd:
        flags.append(f"--cmd {cmd}")
    if hook and hook != "pwd":
        flags.append(f"--hook {hook}")
    if no_cmd:
        flags.append("--no-cmd")

    flag_text = f" {' '.join(flags)}" if flags else ""

    if shell == "powershell":
        return (
            f"Invoke-Expression (& {{ (zoxide init powershell{flag_text}) }})"
        )
    if shell == "bash":
        return f'eval "$(zoxide init bash{flag_text})"'

    raise ValueError(f"Unsupported shell: {shell}")


def update_profile_content(
    existing_text: str, desired_line: str
) -> tuple[str, bool]:
    current_lines = existing_text.splitlines()
    matching_lines = [
        line
        for line in current_lines
        if "zoxide init" in line and not line.lstrip().startswith("#")
    ]
    updated_lines = [
        line
        for line in current_lines
        if "zoxide init" not in line or line.lstrip().startswith("#")
    ]

    if (
        matching_lines == [desired_line]
        and len(updated_lines) == len(current_lines) - 1
    ):
        return existing_text, False

    updated_lines.append(desired_line)
    updated_text = "\n".join(updated_lines) + "\n"
    return updated_text, updated_text != existing_text


def update_profile_file(
    profile_path: Path, desired_line: str, dry_run: bool
) -> bool:
    existing_text = ""
    if profile_path.exists():
        with open(profile_path, "r", encoding="utf-8") as handle:
            existing_text = handle.read()

    updated_text, changed = update_profile_content(existing_text, desired_line)

    if not changed:
        return False

    if dry_run:
        if profile_path.exists():
            log(f"Would update profile: {profile_path}")
        else:
            log(f"Would create profile: {profile_path}")
        log(f"Would set init line: {desired_line}")
        return True

    profile_path.parent.mkdir(parents=True, exist_ok=True)
    with open(profile_path, "w", encoding="utf-8") as handle:
        handle.write(updated_text)

    if profile_path.exists():
        log(f"Updated profile: {profile_path}")
    else:
        log(f"Created profile: {profile_path}")

    return True


def run_setx(var_name: str, value: str) -> None:
    if sys.platform != "win32":
        log(f"Skipping setx for {var_name} on non-Windows platform")
        return

    result = subprocess.run(
        ["setx", var_name, value], capture_output=True, text=True
    )
    if result.returncode != 0:
        stderr = (result.stderr or result.stdout or "setx failed").strip()
        raise RuntimeError(f"setx {var_name} failed: {stderr}")


def check_installed(_args: dict, request_id: str) -> dict:
    installed = (
        shutil.which("zoxide.exe") is not None
        or shutil.which("zoxide") is not None
    )
    return {
        "requestId": request_id,
        "success": True,
        "changed": False,
        "data": {"installed": installed},
    }


def apply_config(args: dict, dry_run: bool, request_id: str) -> dict:
    changed = False

    env_vars = args.get("env_vars", {})
    init_args = args.get("init", {})

    for var_name in ENV_VARS:
        if var_name not in env_vars:
            continue

        requested_value = str(env_vars[var_name])
        current_value = os.environ.get(var_name)

        if current_value == requested_value:
            continue

        changed = True
        if dry_run:
            log(f"Would set {var_name} to {requested_value}")
        else:
            log(f"Setting {var_name} to {requested_value}")
            run_setx(var_name, requested_value)

    ps_profile = get_powershell_profile_path()
    bash_profile = get_bash_profile_path()
    ps_line = build_init_line("powershell", init_args)
    bash_line = build_init_line("bash", init_args)

    if update_profile_file(ps_profile, ps_line, dry_run):
        changed = True
    if update_profile_file(bash_profile, bash_line, dry_run):
        changed = True

    return {
        "requestId": request_id,
        "success": True,
        "changed": changed,
    }


def process_request(request: dict) -> dict:
    request_id = request.get("requestId", "unknown")
    command = request.get("command")
    args = request.get("args", {})
    context = request.get("context", {})
    dry_run = bool(request.get("dry_run", context.get("dryRun", False)))

    try:
        if command == "check_installed":
            return check_installed(args, request_id)
        if command == "apply":
            return apply_config(args, dry_run, request_id)
        return {
            "requestId": request_id,
            "success": False,
            "changed": False,
            "error": f"Unknown command: {command}",
        }
    except Exception as exc:
        log(f"Failed to handle command '{command}': {exc}")
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
        response = {
            "requestId": "unknown",
            "success": False,
            "changed": False,
            "error": f"Invalid JSON: {exc}",
        }
        sys.stdout.write(json.dumps(response) + "\n")
        sys.stdout.flush()
        return

    response = process_request(request)
    sys.stdout.write(json.dumps(response) + "\n")
    sys.stdout.flush()


if __name__ == "__main__":
    main()
