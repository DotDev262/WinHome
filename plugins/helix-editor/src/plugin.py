import sys
import json
import os
import shutil

try:
    import tomllib
except ImportError:
    tomllib = None


def log(msg):
    sys.stderr.write(f"[helix-plugin] {msg}\n")
    sys.stderr.flush()


def get_helix_dir():
    appdata = os.getenv("APPDATA")
    if not appdata:
        # Fallback if APPDATA isn't set, though it should be on Windows
        userprofile = os.getenv("USERPROFILE")
        if userprofile:
            appdata = os.path.join(userprofile, "AppData", "Roaming")
        else:
            raise Exception("APPDATA environment variable not found")
    
    helix_dir = os.path.join(appdata, "helix")
    os.makedirs(helix_dir, exist_ok=True)
    
    return helix_dir


def read_toml(file_path: str) -> dict:
    if not os.path.exists(file_path):
        return {}
    
    if tomllib:
        try:
            with open(file_path, "rb") as f:
                return tomllib.load(f)
        except Exception as e:
            log(f"Warning: could not parse {file_path} using tomllib: {e}")
            return {}
    else:
        log("Warning: tomllib not available (requires Python 3.11+). Starting with empty config.")
        return {}


def dump_value(v):
    if isinstance(v, bool):
        return "true" if v else "false"
    elif isinstance(v, str):
        return json.dumps(v, ensure_ascii=False)
    elif isinstance(v, (int, float)):
        return str(v)
    elif isinstance(v, list):
        items = ", ".join(dump_value(item) for item in v)
        return f"[{items}]"
    else:
        return json.dumps(v, ensure_ascii=False)


def dump_toml(data: dict) -> str:
    lines = []
    
    # Primitives first
    for k, v in data.items():
        if not isinstance(v, dict) and not (isinstance(v, list) and len(v) > 0 and isinstance(v[0], dict)):
            lines.append(f"{k} = {dump_value(v)}")
    
    # Array of Tables
    for k, v in data.items():
        if isinstance(v, list) and len(v) > 0 and isinstance(v[0], dict):
            for item in v:
                lines.append("")
                lines.append(f"[[{k}]]")
                for sub_k, sub_v in item.items():
                    if isinstance(sub_v, dict):
                        # E.g. [language.formatter] inside [[language]]
                        # Actually TOML doesn't allow inline tables with JSON colon.
                        # We can just output it as [k.sub_k] or we can just convert dict to inline TOML
                        inline_pairs = [f'{json.dumps(ik, ensure_ascii=False)} = {dump_value(iv)}' for ik, iv in sub_v.items()]
                        lines.append(f"{sub_k} = {{ {', '.join(inline_pairs)} }}")
                    else:
                        lines.append(f"{sub_k} = {dump_value(sub_v)}")

    # Tables after
    for k, v in data.items():
        if isinstance(v, dict):
            lines.append("")
            lines.append(f"[{k}]")
            for sub_k, sub_v in v.items():
                if isinstance(sub_v, dict):
                    # Basic nesting support up to 2 levels
                    lines.append(f"[{k}.{sub_k}]")
                    for sub_sub_k, sub_sub_v in sub_v.items():
                        lines.append(f"{sub_sub_k} = {dump_value(sub_sub_v)}")
                else:
                    lines.append(f"{sub_k} = {dump_value(sub_v)}")
                
    return "\n".join(lines).strip() + "\n"


def write_toml(file_path: str, data: dict) -> None:
    os.makedirs(os.path.dirname(file_path), exist_ok=True)
    toml_str = dump_toml(data)
    with open(file_path, "w", encoding="utf-8") as f:
        f.write(toml_str)


def merge_settings(target: dict, source: dict) -> bool:
    changed = False
    for key, value in source.items():
        if isinstance(value, dict):
            if key not in target or not isinstance(target.get(key), dict):
                target[key] = {}
                changed = True
            
            # Recursive merge for deep dictionaries
            if merge_settings(target[key], value):
                changed = True
        elif isinstance(value, list) and len(value) > 0 and isinstance(value[0], dict):
            # Array of tables merge (e.g., [[language]])
            if key not in target:
                target[key] = []
            
            # Simple replacement or append logic for arrays of tables based on "name"
            for item in value:
                if "name" in item:
                    existing_item = next((i for i in target[key] if isinstance(i, dict) and i.get("name") == item["name"]), None)
                    if existing_item:
                        if merge_settings(existing_item, item):
                            changed = True
                    else:
                        target[key].append(item)
                        changed = True
                else:
                    target[key].append(item)
                    changed = True
        else:
            if key not in target or target[key] != value:
                target[key] = value
                changed = True
    return changed


def check_installed(args: dict, request_id: str) -> dict:
    installed = shutil.which("hx.exe") is not None or shutil.which("hx") is not None
    return {
        "requestId": request_id,
        "success": True,
        "changed": False,
        "data": {"installed": installed},
    }


def apply_config(args: dict, context: dict, request_id: str) -> dict:
    dry_run = context.get("dryRun", False)
    
    config_settings = args.get("config", {})
    language_settings = args.get("languages", {})

    try:
        helix_dir = get_helix_dir()
        config_path = os.path.join(helix_dir, "config.toml")
        languages_path = os.path.join(helix_dir, "languages.toml")
        
        changed = False
        
        # Handle config.toml
        if config_settings:
            current_config = read_toml(config_path)
            if merge_settings(current_config, config_settings):
                changed = True
                if not dry_run:
                    write_toml(config_path, current_config)
                    log(f"Updated config: {config_path}")
                    
        # Handle languages.toml
        if language_settings:
            current_languages = read_toml(languages_path)
            if merge_settings(current_languages, language_settings):
                changed = True
                if not dry_run:
                    write_toml(languages_path, current_languages)
                    log(f"Updated languages: {languages_path}")

        if not changed:
            return {
                "requestId": request_id,
                "success": True,
                "changed": False,
            }

        if dry_run:
            log(f"Would update Helix config with new settings")
            return {
                "requestId": request_id,
                "success": True,
                "changed": True,
            }

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
