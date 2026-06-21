import sys
import os
import json
import tempfile

def deep_merge(base, update):
    if not isinstance(base, dict) or not isinstance(update, dict):
        return update
    for key, val in update.items():
        if key in base and isinstance(base[key], dict) and isinstance(val, dict):
            base[key] = deep_merge(base[key], val)
        else:
            base[key] = val
    return base

def main():
    raw_input = sys.stdin.read().strip()
    if not raw_input:
        print(json.dumps({"error": "Empty stdin context payload received"}), file=sys.stderr)
        sys.exit(1)
        
    try:
        args = json.loads(raw_input)
    except json.JSONDecodeError:
        print(json.dumps({"error": "Invalid JSON format payload structure"}), file=sys.stderr)
        sys.exit(1)
        
    request_id = args.get("requestId", "")
    
    if args.get("check_installed", False):
        appdata = os.environ.get("APPDATA", "")
        config_path = os.path.join(appdata, "GitHub Desktop", "config.json") if appdata else ""
        installed = bool(config_path and os.path.exists(config_path))
        print(json.dumps({"requestId": request_id, "installed": installed}))
        sys.exit(0)
        
    settings = args.get("settings", {})
    dry_run = args.get("dryRun", False)
    
    appdata = os.environ.get("APPDATA", "")
    if not appdata:
        print(json.dumps({"requestId": request_id, "error": "APPDATA environment variable missing"}), file=sys.stderr)
        sys.exit(1)
        
    config_dir = os.path.join(appdata, "GitHub Desktop")
    config_path = os.path.join(config_dir, "config.json")
    
    current_config = {}
    if os.path.exists(config_path):
        try:
            with open(config_path, "r", encoding="utf-8") as f:
                current_config = json.load(f)
        except Exception:
            current_config = {}
            
    updated_config = deep_merge(current_config, settings)
    
    if not dry_run:
        if not os.path.exists(config_dir):
            os.makedirs(config_dir, exist_ok=True)
        try:
            fd, temp_path = tempfile.mkstemp(dir=config_dir, prefix="config_", suffix=".json")
            with os.fdopen(fd, "w", encoding="utf-8") as f:
                json.dump(updated_config, f, indent=2)
            os.replace(temp_path, config_path)
        except Exception as e:
            print(json.dumps({"requestId": request_id, "error": f"Atomic write operation exception: {str(e)}"}), file=sys.stderr)
            sys.exit(1)
            
    print(json.dumps({"requestId": request_id}))

if __name__ == "__main__":
    main()
