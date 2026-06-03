import sys
import json
import os
import uuid
import shutil
import tempfile
from pathlib import Path

SETTING_MAP = {
    "channels": "channels",
    "channelAlias": "channel_alias",
    "sslVerify": "ssl_verify",
    "proxyServers": "proxy_servers",
    "envsDirs": "envs_dirs",
    "pkgsDirs": "pkgs_dirs",
    "autoUpdateConda": "auto_update_conda",
    "autoActivateBase": "auto_activate_base",
    "anacondaUpload": "anaconda_upload",
    "reportErrors": "report_errors",
    "pipInteropEnabled": "pip_interop_enabled",
    "maxParallelDownloads": "max_parallel_downloads",
    "privateEnvs": "private_envs",
    "modifyPath": "modify_path"
}

def send_response(request_id, data=None, changed=False, error=None):
    response = {
        "requestId": request_id,
        "data": data,
        "changed": changed
    }
    if error:
        response["error"] = error
        
    print(json.dumps(response))

def check_installed():
    return shutil.which("conda") is not None

def main():
    input_data = sys.stdin.read().strip()
    if not input_data:
        send_response("unknown", error="Empty input received from host.")
        return
        
    try:
        request = json.loads(input_data)
    except json.JSONDecodeError:
        send_response("unknown", error="Invalid JSON input")
        return

    request_id = request.get("requestId") or "unknown"
    command = request.get("command")
    args = request.get("args", {})

    if command == "check_installed":
        is_installed = check_installed()
        send_response(request_id, data=is_installed, changed=False)
        return

    if command == "apply":
        try:
            import yaml 
        except ImportError:
            send_response(request_id, error="PyYAML is missing. Please install it.")
            return

        condarc_path = Path.home() / ".condarc"
        dry_run = args.get("dryRun", False)
        settings = args.get("settings", {})
        
        if not isinstance(settings, dict):
            send_response(request_id, error="Settings must be a dictionary.")
            return

        current_config = {}
        if condarc_path.exists():
            with open(condarc_path, "r", encoding="utf-8") as f:
                loaded = yaml.safe_load(f)
                if isinstance(loaded, dict):
                    current_config = loaded

        changed = False
        for key, value in settings.items():
            if key in SETTING_MAP:
                conda_key = SETTING_MAP[key]
                if current_config.get(conda_key) != value:
                    current_config[conda_key] = value
                    changed = True

        if dry_run:
            send_response(request_id, data={"status": "dry-run complete"}, changed=changed)
            return

        if changed:
            if condarc_path.exists():
                backup_path = condarc_path.with_name(f".condarc.bak.{uuid.uuid4().hex}")
                shutil.copy2(condarc_path, backup_path)
            
            # FIX: Atomic file write using mkstemp and os.replace
            fd, tmp_path = tempfile.mkstemp(dir=str(condarc_path.parent), text=True)
            try:
                with os.fdopen(fd, "w", encoding="utf-8", newline="\n") as f:
                    yaml.dump(current_config, f, default_flow_style=False)
                    f.write("\n") 
                os.replace(tmp_path, condarc_path)
            except Exception:
                os.remove(tmp_path)
                raise

        send_response(request_id, data={"status": "success"}, changed=changed)
        return

    send_response(request_id, error=f"Unknown command: {command}")

if __name__ == "__main__":
    main()
