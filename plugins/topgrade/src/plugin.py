# /// script
# requires-python = ">=3.11"
# dependencies = [
#     "tomlkit",
# ]
# ///

import sys
import json
import os
import shutil
import tomlkit

def get_topgrade_config_path():
    appdata = os.environ.get("APPDATA")
    if not appdata:
        return None
    
    # Try %APPDATA%\topgrade.toml
    fallback_path = os.path.join(appdata, "topgrade.toml")
    # Try %APPDATA%\topgrade\topgrade.toml
    main_path = os.path.join(appdata, "topgrade", "topgrade.toml")
    
    if os.path.exists(main_path):
        return main_path
    elif os.path.exists(fallback_path):
        return fallback_path
    
    # Default to main path if neither exists
    return main_path

def merge_dict(target, source):
    """Recursively merge source dictionary into target tomlkit document/table."""
    for k, v in source.items():
        if isinstance(v, dict):
            if k not in target:
                target[k] = tomlkit.table()
            if isinstance(target[k], dict):
                merge_dict(target[k], v)
            else:
                target[k] = v
        else:
            target[k] = v

def handle_check_installed():
    topgrade_path = shutil.which("topgrade")
    return {
        "success": True,
        "changed": False,
        "data": {
            "installed": topgrade_path is not None
        }
    }

def handle_apply(args, context):
    settings = args.get("settings", {})
    if not settings:
        return {"success": True, "changed": False}
        
    config_path = get_topgrade_config_path()
    if not config_path:
        return {"success": False, "changed": False, "error": "APPDATA environment variable not set"}
        
    dry_run = context.get("dryRun", False)
    
    if os.path.exists(config_path):
        with open(config_path, "r", encoding="utf-8") as f:
            try:
                doc = tomlkit.load(f)
            except Exception as e:
                return {"success": False, "changed": False, "error": f"Failed to parse topgrade config: {str(e)}"}
    else:
        doc = tomlkit.document()
        # Create directory if it doesn't exist
        os.makedirs(os.path.dirname(config_path), exist_ok=True)
        
    orig_content = doc.as_string()
    
    merge_dict(doc, settings)
    
    new_content = doc.as_string()
    changed = (orig_content != new_content)
    
    if changed and not dry_run:
        try:
            with open(config_path, "w", encoding="utf-8") as f:
                f.write(new_content)
        except Exception as e:
            return {"success": False, "changed": False, "error": f"Failed to write config: {str(e)}"}
            
    if changed and dry_run:
        sys.stderr.write(f"Would update topgrade config at {config_path}\n")
        
    return {"success": True, "changed": changed}

def main():
    raw_input = sys.stdin.read()
    if not raw_input:
        return
        
    try:
        request = json.loads(raw_input)
    except json.JSONDecodeError:
        sys.stderr.write("Failed to parse JSON input\n")
        return
        
    cmd = request.get("command")
    req_id = request.get("requestId")
    args = request.get("args", {})
    context = request.get("context", {})
    
    response = {
        "requestId": req_id,
        "success": False,
        "changed": False
    }
    
    if cmd == "check_installed":
        res = handle_check_installed()
        response.update(res)
    elif cmd == "apply":
        res = handle_apply(args, context)
        response.update(res)
    else:
        response["error"] = f"Unknown command: {cmd}"
        
    print(json.dumps(response))

if __name__ == "__main__":
    main()
