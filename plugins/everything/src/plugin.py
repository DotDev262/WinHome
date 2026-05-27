import sys
import json
import os
import configparser
from pathlib import Path
from typing import Dict, Any

EVERYTHING_INI_PATH = Path(os.getenv('APPDATA', '')) / 'Everything' / 'Everything.ini'

def read_ini() -> Dict:
    if not EVERYTHING_INI_PATH.exists():
        return {}
    
    config = configparser.ConfigParser()
    config.read(str(EVERYTHING_INI_PATH), encoding='utf-8')
    
    result = {}
    for section in config.sections():
        result[section] = dict(config.items(section))
    return result

def write_ini(config: Dict, dry_run: bool = False):
    if dry_run:
        print('[DRY-RUN] Would write the following config:', file=sys.stderr)
        print(json.dumps(config, indent=2), file=sys.stderr)
        return
    
    EVERYTHING_INI_PATH.parent.mkdir(parents=True, exist_ok=True)
    
    parser = configparser.ConfigParser()
    for section, values in config.items():
        parser[section] = {str(k): str(v).lower() if isinstance(v, bool) else str(v) 
                          for k, v in values.items()}
    
    with open(EVERYTHING_INI_PATH, 'w', encoding='utf-8') as f:
        parser.write(f)

def apply(request: Dict) -> Dict:
    args = request.get('args', {})
    dry_run = args.get('dry_run', False)
    new_settings = args.get('args', {})   # contains general, search, indexes, etc.

    current = read_ini()
    
    # Most Everything settings go under [Everything] section
    if 'Everything' not in current:
        current['Everything'] = {}
    
    # Flatten all input sections into the main [Everything] section
    for section_data in new_settings.values():
        if isinstance(section_data, dict):
            current['Everything'].update({str(k): str(v) for k, v in section_data.items()})

    write_ini(current, dry_run)
    
    return {
        "success": True,
        "changed": True
    }

def check_installed() -> Dict:
    installed = (EVERYTHING_INI_PATH.parent.exists() or 
                Path(r'C:\Program Files\Everything').exists() or
                Path(r'C:\Program Files (x86)\Everything').exists())
    return {"installed": installed}

def main():
    input_data = sys.stdin.read().strip()
    if not input_data:
        print(json.dumps({'error': 'No input received'}))
        return

    try:
        req = json.loads(input_data)
        cmd = req.get('command')
        
        if cmd == 'check_installed':
            resp = check_installed()
        elif cmd == 'apply':
            resp = apply(req)
        else:
            resp = {'error': f'Unknown command: {cmd}'}
        
        print(json.dumps(resp))
    except json.JSONDecodeError as e:
        print(json.dumps({'error': f'Invalid JSON: {str(e)}'}))
    except Exception as e:
        print(json.dumps({'error': str(e)}))

if __name__ == '__main__':
    main()
