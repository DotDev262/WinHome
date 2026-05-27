import sys
import json
import os
import configparser
from pathlib import Path

EVERYTHING_INI_PATH = Path(os.getenv('APPDATA', '')) / 'Everything' / 'Everything.ini'

def read_ini() -> dict:
    if not EVERYTHING_INI_PATH.exists():
        return {}
    config = configparser.ConfigParser()
    config.read(str(EVERYTHING_INI_PATH), encoding='utf-8')
    return {section: dict(config.items(section)) for section in config.sections()}

def write_ini(config: dict, dry_run: bool = False):
    if dry_run:
        print('[DRY-RUN] Would write the following to Everything.ini:', file=sys.stderr)
        print(json.dumps(config, indent=2), file=sys.stderr)
        return

    EVERYTHING_INI_PATH.parent.mkdir(parents=True, exist_ok=True)
    
    parser = configparser.ConfigParser()
    for section, values in config.items():
        parser[section] = {k: str(v).lower() if isinstance(v, bool) else str(v) 
                          for k, v in values.items()}
    
    with open(EVERYTHING_INI_PATH, 'w', encoding='utf-8') as f:
        parser.write(f)

def apply(request: dict) -> dict:
    args = request.get('args', {})
    dry_run = args.get('dry_run', False)
    new_settings = args.get('args', {})        # 'general', 'search', etc.

    current = read_ini()
    changed = False

    for section, values in new_settings.items():
        if section not in current:
            current[section] = {}
        
        for key, value in values.items():
            old_value = current[section].get(key)
            if str(old_value).lower() != str(value).lower():
                changed = True
            current[section][key] = value

    write_ini(current, dry_run)
    
    return {
        "success": True,
        "changed": changed
    }

def check_installed() -> dict:
    installed = EVERYTHING_INI_PATH.parent.exists()
    return {"installed": installed}

def main():
    input_data = sys.stdin.read().strip()
    if not input_data:
        print(json.dumps({"error": "No input"}))
        return

    try:
        req = json.loads(input_data)
        cmd = req.get("command")

        if cmd == "check_installed":
            resp = check_installed()
        elif cmd == "apply":
            resp = apply(req)
        else:
            resp = {"error": f"Unknown command: {cmd}"}

        print(json.dumps(resp))
    except Exception as e:
        print(json.dumps({"error": str(e)}))

if __name__ == "__main__":
    main()
