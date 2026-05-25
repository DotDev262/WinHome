import json
import os
import shutil
import sys


def log(msg: str) -> None:
    sys.stderr.write(f"[openssh-plugin] {msg}\n")
    sys.stderr.flush()


def get_config_path() -> str:
    return os.path.join(get_ssh_dir(), "config")


def get_ssh_dir() -> str:
    user_profile = os.getenv("USERPROFILE") or os.path.expanduser("~")
    if not user_profile:
        raise Exception("Could not determine user profile directory")
    return os.path.join(user_profile, ".ssh")


def normalize_value(value) -> str:
    if isinstance(value, bool):
        return "yes" if value else "no"
    return str(value)


def parse_ssh_config(content: str) -> dict:
    config = {"global": {}, "hosts": {}}
    current_host = None

    for raw_line in content.splitlines():
        line = raw_line.strip()
        if not line or line.startswith("#"):
            continue

        parts = line.split(None, 1)
        key = parts[0]
        value = parts[1].strip() if len(parts) > 1 else ""

        if key.lower() == "host":
            current_host = value
            config["hosts"].setdefault(current_host, {})
            continue

        target = config["hosts"][current_host] if current_host else config["global"]
        target[key] = value

    return config


def serialize_ssh_config(config: dict) -> str:
    lines = []

    for key, value in config.get("global", {}).items():
        lines.append(f"{key} {normalize_value(value)}")

    if lines and config.get("hosts"):
        lines.append("")

    for index, (host, settings) in enumerate(config.get("hosts", {}).items()):
        if index > 0:
            lines.append("")
        lines.append(f"Host {host}")
        for key, value in settings.items():
            lines.append(f"  {key} {normalize_value(value)}")

    return "\n".join(lines) + ("\n" if lines else "")


def read_config(file_path: str) -> dict:
    if not os.path.exists(file_path):
        return {"global": {}, "hosts": {}}

    with open(file_path, "r", encoding="utf-8") as f:
        return parse_ssh_config(f.read())


def write_config(file_path: str, config: dict) -> None:
    os.makedirs(os.path.dirname(file_path), exist_ok=True)
    with open(file_path, "w", encoding="utf-8") as f:
        f.write(serialize_ssh_config(config))


def merge_section(target: dict, source: dict) -> bool:
    changed = False

    for key, value in source.items():
        normalized = normalize_value(value)
        if key not in target or normalize_value(target[key]) != normalized:
            target[key] = normalized
            changed = True

    return changed


def merge_config(current: dict, updates: dict) -> bool:
    changed = False
    global_settings = updates.get("global", {})
    hosts = updates.get("hosts", {})

    if global_settings and merge_section(current.setdefault("global", {}), global_settings):
        changed = True

    current_hosts = current.setdefault("hosts", {})
    for host, settings in hosts.items():
        if host not in current_hosts:
            current_hosts[host] = {}
            changed = True
        if merge_section(current_hosts[host], settings):
            changed = True

    return changed


def check_installed(args: dict, request_id: str) -> dict:
    ssh_path = shutil.which("ssh.exe") or shutil.which("ssh")
    return {
        "requestId": request_id,
        "success": True,
        "changed": False,
        "data": {
            "installed": ssh_path is not None,
            "sshPath": ssh_path,
            "configDirExists": os.path.isdir(get_ssh_dir()),
        },
    }


def apply_config(args: dict, context: dict, request_id: str) -> dict:
    dry_run = context.get("dryRun", False)

    try:
        config_path = get_config_path()
        current_config = read_config(config_path)
        changed = merge_config(current_config, args)

        if not changed:
            return {
                "requestId": request_id,
                "success": True,
                "changed": False,
            }

        if dry_run:
            log(f"Would update {config_path} with OpenSSH client settings")
            return {
                "requestId": request_id,
                "success": True,
                "changed": False,
            }

        write_config(config_path, current_config)
        log(f"Updated OpenSSH client config: {config_path}")

        return {
            "requestId": request_id,
            "success": True,
            "changed": True,
        }
    except Exception as e:
        log(f"Failed to apply config: {e}")
        return {
            "requestId": request_id,
            "success": False,
            "changed": False,
            "error": str(e),
        }


def main() -> None:
    input_data = sys.stdin.read()
    if not input_data:
        return

    try:
        request = json.loads(input_data)
    except Exception as e:
        log(f"Failed to parse request: {e}")
        sys.exit(1)

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
