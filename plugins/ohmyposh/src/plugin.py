import json
import os
import sys

# Dynamic Profile Discovery

_MODERN_PATH = os.path.expandvars(
    r"%USERPROFILE%\Documents\PowerShell\Microsoft.PowerShell_profile.ps1"
)
_LEGACY_PATH = os.path.expandvars(
    r"%USERPROFILE%\Documents\WindowsPowerShell\Microsoft.PowerShell_profile.ps1"
)

# Default modern path, fallback to legacy
PROFILE_PATH = (
    _MODERN_PATH
    if os.path.exists(os.path.dirname(_MODERN_PATH))
    else _LEGACY_PATH
)

OMP_BEGIN = "# OH-MY-POSH-PLUGIN BEGIN"
OMP_END = "# OH-MY-POSH-PLUGIN END"

# Helpers


def log(msg):
    sys.stderr.write(f"[oh-my-posh-plugin] {msg}\n")
    sys.stderr.flush()


def build_omp_line(theme: str) -> str:
    """Pass token string to the --config flag"""
    return f'oh-my-posh init pwsh --config "{theme}" | Invoke-Expression'


def read_profile(profile_path: str) -> str:
    if not os.path.exists(profile_path):
        return ""
    with open(profile_path, "r", encoding="utf-8") as f:
        return f.read()


def write_profile(profile_path: str, content: str) -> None:
    os.makedirs(os.path.dirname(profile_path), exist_ok=True)
    with open(profile_path, "w", encoding="utf-8") as f:
        f.write(content)


# Commands


def apply_config(args: dict, context: dict, request_id: str) -> dict:
    theme = args.get("theme") or args.get("settings", {}).get("theme")
    if not theme:
        return {
            "requestId": request_id,
            "success": False,
            "changed": False,
            "error": "No theme specified",
        }

    profile_path = (
        args.get("profile")
        or args.get("settings", {}).get("profile")
        or PROFILE_PATH
    )
    desired_line = build_omp_line(theme)
    current_content = read_profile(profile_path)

    omp_block = f"{OMP_BEGIN}\n{desired_line}\n{OMP_END}"

    if context.get("dryRun"):
        theme_change = omp_block not in current_content
        return {
            "requestId": request_id,
            "success": True,
            "changed": theme_change,
        }

    # If omp block already there, return
    if omp_block in current_content:
        return {"requestId": request_id, "success": True, "changed": False}

    try:
        idx_begin = current_content.find(OMP_BEGIN)
        idx_end = current_content.find(OMP_END)
        # If an old block exists, swap entirely
        if idx_begin != -1 and idx_end != -1 and idx_begin < idx_end:
            # split current_content around OMP_BEGIN and OMP_END
            before_omp = current_content[:idx_begin]
            after_omp = current_content[idx_end + len(OMP_END) :].lstrip("\r\n")
            new_profile = before_omp + omp_block + "\n" + after_omp
            log(f"Updated theme to {theme}")
        else:
            # first time setting
            new_profile = (
                current_content.rstrip("\n")
                + ("\n\n" if current_content else "")
                + omp_block
                + "\n"
            )
            log(f"Created new omp block for {theme}")

        write_profile(profile_path, new_profile)
        return {"requestId": request_id, "success": True, "changed": True}

    except Exception as e:
        return {
            "requestId": request_id,
            "success": False,
            "changed": False,
            "error": str(e),
        }


def check_installed(args: dict, request_id: str) -> dict:
    theme = args.get("theme") or args.get("settings", {}).get("theme")
    profile_path = (
        args.get("profile")
        or args.get("settings", {}).get("profile")
        or PROFILE_PATH
    )
    current_content = read_profile(profile_path)

    if theme:
        desired_line = build_omp_line(theme)
        omp_block = f"{OMP_BEGIN}\n{desired_line}\n{OMP_END}"
        installed = omp_block in current_content
    else:
        idx_begin = current_content.find(OMP_BEGIN)
        idx_end = current_content.find(OMP_END)
        installed = idx_begin != -1 and idx_end != -1 and idx_begin < idx_end
        if not installed:
            log("No active OMP plugin block found")

    return {
        "requestId": request_id,
        "success": True,
        "changed": False,
        "data": installed,
    }


# Main


def main():
    input_data = sys.stdin.read()
    if not input_data:
        return

    try:
        request = json.loads(input_data)
    except Exception as e:
        log(f"Failed to parse JSON request payload: {e}")
        sys.exit(1)

    request_id = request.get("requestId", "unknown")
    command = request.get("command")
    args = request.get("args", {})
    context = request.get("context", {})

    response = {"requestId": request_id, "success": False, "changed": False}

    try:
        if command == "apply":
            response = apply_config(args, context, request_id)
        elif command == "check_installed":
            response = check_installed(args, request_id)
        else:
            response["error"] = f"Unknown plugin command instruction: {command}"
    except Exception as fatal_err:
        response["error"] = (
            f"Internal Plugin Processing Error: {str(fatal_err)}"
        )

    sys.stdout.write(json.dumps(response) + "\n")
    sys.stdout.flush()


if __name__ == "__main__":
    main()
