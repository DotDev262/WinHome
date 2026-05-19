import json
import os
import sys

LOCALAPPDATA = os.environ.get('LOCALAPPDATA', '')
POWERTOYS_PATH = os.path.join(LOCALAPPDATA, 'Microsoft', 'PowerToys')
GENERAL_SETTINGS_PATH = os.path.join(POWERTOYS_PATH, 'settings.json')

SUPPORTED_MODULES = {
    'fancyzones': 'FancyZones',
    'awake': 'Awake',
    'powerrename': 'PowerRename',
}

def log(msg):
    sys.stderr.write(f'[powertoys-plugin] {msg}\n')

def get_settings_path(module_key):
    folder = SUPPORTED_MODULES.get(module_key.lower())
    if not folder:
        return None
    return os.path.join(POWERTOYS_PATH, folder, 'settings.json')

def read_settings(path):
    if not os.path.exists(path):
        return {}
    try:
        with open(path, 'r', encoding='utf-8') as f:
            return json.load(f)
    except Exception as e:
        log(f'Error reading {path}: {e}')
        return {}

def write_settings(path, data):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, 'w', encoding='utf-8') as f:
        json.dump(data, f, indent=4)
        f.write('\n')

def merge_dict(target, updates):
    changed = False
    for key, value in updates.items():
        if isinstance(value, dict) and isinstance(target.get(key), dict):
            if merge_dict(target[key], value):
                changed = True
        else:
            if target.get(key) != value:
                target[key] = value
                changed = True
    return changed

def apply_general_config(desired, current):
    if not isinstance(desired, dict):
        log('General config must be a dictionary.')
        return False

    changed = False
    raw = desired.get('raw')
    settings = desired.get('settings')

    if isinstance(raw, dict):
        changed = merge_dict(current, raw) or changed

    if isinstance(settings, dict):
        changed = merge_dict(current, settings) or changed

    if raw is None and settings is None:
        changed = merge_dict(current, desired) or changed

    return changed

def apply_module_config(desired, current):
    if not isinstance(desired, dict):
        log('Module config must be a dictionary.')
        return False

    changed = False

    if 'enabled' in desired:
        if current.get('enabled') != desired.get('enabled'):
            current['enabled'] = desired.get('enabled')
            changed = True

    properties = current.get('properties')
    if properties is None or not isinstance(properties, dict):
        properties = {}

    if isinstance(desired.get('settings'), dict):
        changed = merge_dict(properties, desired.get('settings')) or changed
        current['properties'] = properties

    if isinstance(desired.get('properties'), dict):
        changed = merge_dict(properties, desired.get('properties')) or changed
        current['properties'] = properties

    if isinstance(desired.get('raw'), dict):
        changed = merge_dict(current, desired.get('raw')) or changed

    if not any(k in desired for k in ('enabled', 'settings', 'properties', 'raw')):
        changed = merge_dict(current, desired) or changed

    return changed

def normalize_module_config(args):
    modules = {}

    if isinstance(args.get('modules'), dict):
        modules.update(args.get('modules'))

    for module_key in SUPPORTED_MODULES.keys():
        if module_key in args and module_key not in modules:
            modules[module_key] = args.get(module_key)

    return modules

def apply_config(args, context, request_id):
    dry_run = context.get('dryRun', False)
    overall_changed = False
    overall_success = True

    if not LOCALAPPDATA:
        return {
            'requestId': request_id,
            'success': False,
            'changed': False,
            'error': 'LOCALAPPDATA is not set.'
        }

    general_config = args.get('general')
    if general_config is not None:
        current_general = read_settings(GENERAL_SETTINGS_PATH)
        general_changed = apply_general_config(general_config, current_general)

        if general_changed:
            if dry_run:
                log(f'Would update general settings at {GENERAL_SETTINGS_PATH}')
            else:
                try:
                    write_settings(GENERAL_SETTINGS_PATH, current_general)
                    log('Updated general PowerToys settings')
                    overall_changed = True
                except Exception as e:
                    log(f'Error writing general settings: {e}')
                    overall_success = False

    modules = normalize_module_config(args)

    for module_key, desired in modules.items():
        path = get_settings_path(module_key)
        if not path:
            log(f'Unknown module: {module_key}')
            overall_success = False
            continue

        current = read_settings(path)
        changed = apply_module_config(desired, current)

        if not changed:
            log(f'{module_key}: already up to date')
            continue

        if dry_run:
            log(f'Would update {module_key} settings at {path}')
            continue

        try:
            write_settings(path, current)
            log(f'Updated {module_key} settings')
            overall_changed = True
        except Exception as e:
            log(f'Error writing {module_key} settings: {e}')
            overall_success = False

    if overall_changed and not dry_run and os.path.exists(GENERAL_SETTINGS_PATH):
        try:
            os.utime(GENERAL_SETTINGS_PATH, None)
        except Exception as e:
            log(f'Warning: failed to touch general settings: {e}')

    return {
        'requestId': request_id,
        'success': overall_success,
        'changed': overall_changed
    }

def check_installed(args, request_id):
    module_key = args.get('module', '')
    path = get_settings_path(module_key) if module_key else GENERAL_SETTINGS_PATH
    exists = path is not None and os.path.exists(path)
    return {
        'requestId': request_id,
        'success': True,
        'changed': False,
        'data': exists
    }

def main():
    input_data = sys.stdin.read()
    if not input_data:
        return

    try:
        request = json.loads(input_data)
    except Exception as e:
        log(f'Failed to parse request: {e}')
        sys.exit(1)

    command = request.get('command')
    args = request.get('args', {})
    context = request.get('context', {})
    request_id = request.get('requestId', '')

    if command == 'apply':
        response = apply_config(args, context, request_id)
    elif command == 'check_installed':
        response = check_installed(args, request_id)
    else:
        response = {
            'requestId': request_id,
            'success': False,
            'changed': False,
            'error': f'Unknown command: {command}'
        }

    sys.stdout.write(json.dumps(response) + '\n')

if __name__ == '__main__':
    main()
