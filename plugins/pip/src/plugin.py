import sys
import json
import os
import shutil
import configparser

def log(msg):
    sys.stderr.write(f"[pip-plugin] {msg}\n")
    sys.stderr.flush()

def get_pip_ini_path():
    appdata = os.getenv("APPDATA")
    if appdata:
        return os.path.join(appdata, "pip", "pip.ini")
    from pathlib import Path
    return str(Path.home() / ".config" / "pip" / "pip.conf")

def check_installed(args, request_id):
    installed = shutil.which("pip.exe") is not None or shutil.which("pip") is not None
    return {
        "requestId": request_id,
        "success": True,
        "changed": False,
        "data": {"installed": installed},
    }

def apply_config(args, context, request_id):
    dry_run = context.get("dryRun", False)

    try:
        pip_ini_path = get_pip_ini_path()

        config = configparser.ConfigParser()
        # Preserve case of keys
        config.optionxform = str

        if os.path.exists(pip_ini_path):
            config.read(pip_ini_path, encoding="utf-8")
        
        if not config.has_section("global"):
            config.add_section("global")
            
        changed = False
        for key, value in args.items():
            if isinstance(value, bool):
                value = "true" if value else "false"
            else:
                value = str(value)
                
            if not config.has_option("global", key) or config.get("global", key) != value:
                config.set("global", key, value)
                changed = True

        if not changed:
            return {
                "requestId": request_id,
                "success": True,
                "changed": False,
            }

        if dry_run:
            log(f"Would update {pip_ini_path} with: {json.dumps(args)}")
            return {
                "requestId": request_id,
                "success": True,
                "changed": True,
            }

        os.makedirs(os.path.dirname(pip_ini_path), exist_ok=True)
        temp_path = pip_ini_path + ".tmp"
        try:
            with open(temp_path, "w", encoding="utf-8") as f:
                config.write(f)
            os.replace(temp_path, pip_ini_path)
        except Exception:
            if os.path.exists(temp_path):
                os.remove(temp_path)
            raise

        log(f"Updated pip config: {pip_ini_path}")
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

def main():
    input_data = sys.stdin.read()
    if not input_data:
        return

    try:
        request = json.loads(input_data)
    except Exception as e:
        log(f"Failed to parse request: {e}")
        sys.stdout.write(json.dumps({
            "requestId": "unknown", 
            "success": False, 
            "changed": False, 
            "error": f"Failed to parse request: {e}"
        }) + "\n")
        sys.stdout.flush()
        return

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

    except Exception as e:
        response["error"] = f"Internal Script Error: {str(e)}"

    sys.stdout.write(json.dumps(response) + "\n")
    sys.stdout.flush()

if __name__ == "__main__":
    main()
