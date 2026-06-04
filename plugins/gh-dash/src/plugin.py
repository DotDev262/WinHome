# /// script
# dependencies = [
#   "pyyaml",
# ]
# ///

import json
import os
import re
import shutil
import subprocess
import sys

try:
    import yaml as _yaml

    _HAS_PYYAML = True
except ImportError:
    _yaml = None
    _HAS_PYYAML = False


# Minimal stdlib YAML fallback used when PyYAML is unavailable.
# Covers the gh-dash config subset: scalar key-values, nested dicts,
# and block sequences of dicts (prSections / issuesSections).

_KEY_RE = re.compile(r"^([^:\s][^:]*):\s*(.*)")


def _parse_scalar(val: str):
    val = val.strip()
    if not val or val in ("null", "~"):
        return None
    if val.lower() == "true":
        return True
    if val.lower() == "false":
        return False
    if (val.startswith('"') and val.endswith('"')) or (val.startswith("'") and val.endswith("'")):
        return val[1:-1]
    try:
        return int(val)
    except ValueError:
        pass
    try:
        return float(val)
    except ValueError:
        pass
    return val


def _parse_mapping(lines: list, i: int, base_indent: int) -> tuple:
    result = {}
    while i < len(lines):
        raw = lines[i]
        stripped = raw.strip()
        if not stripped or stripped.startswith("#"):
            i += 1
            continue
        indent = len(raw) - len(raw.lstrip())
        if indent < base_indent:
            break
        lstripped = raw.lstrip()
        if lstripped.startswith("- "):
            break
        m = _KEY_RE.match(lstripped)
        if not m:
            i += 1
            continue
        key, val_str = m.group(1).strip(), m.group(2).strip()
        if val_str:
            result[key] = _parse_scalar(val_str)
            i += 1
        else:
            j = i + 1
            while j < len(lines) and not lines[j].strip():
                j += 1
            if j < len(lines) and (len(lines[j]) - len(lines[j].lstrip())) > indent:
                child_indent = len(lines[j]) - len(lines[j].lstrip())
                if lines[j].lstrip().startswith("- "):
                    result[key], i = _parse_sequence(lines, j, child_indent)
                else:
                    result[key], i = _parse_mapping(lines, j, child_indent)
            else:
                result[key] = None
                i = j
    return result, i


def _parse_sequence(lines: list, i: int, base_indent: int) -> tuple:
    items = []
    while i < len(lines):
        raw = lines[i]
        if not raw.strip() or raw.strip().startswith("#"):
            i += 1
            continue
        indent = len(raw) - len(raw.lstrip())
        if indent < base_indent:
            break
        lstripped = raw.lstrip()
        if not lstripped.startswith("- "):
            break
        item_content = lstripped[2:].strip()
        m = _KEY_RE.match(item_content)
        if m:
            key0, val0 = m.group(1).strip(), m.group(2).strip()
            item_dict = {key0: _parse_scalar(val0) if val0 else None}
            j, cont_indent = i + 1, base_indent + 2
            while j < len(lines):
                craw = lines[j]
                if not craw.strip() or craw.strip().startswith("#"):
                    j += 1
                    continue
                if (len(craw) - len(craw.lstrip())) < cont_indent or craw.lstrip().startswith("- "):
                    break
                cm = _KEY_RE.match(craw.lstrip())
                if cm:
                    item_dict[cm.group(1).strip()] = _parse_scalar(cm.group(2).strip()) if cm.group(2).strip() else None
                j += 1
            i = j
            items.append(item_dict)
        elif item_content:
            items.append(_parse_scalar(item_content))
            i += 1
        else:
            items.append(None)
            i += 1
    return items, i


def _scalar_str(val) -> str:
    if val is None:
        return "null"
    if isinstance(val, bool):
        return "true" if val else "false"
    if isinstance(val, (int, float)):
        return str(val)
    s = str(val)
    if not s or re.search(r"[:#\[\]{}|>&*!,@`]", s) or s[0] in (" ", '"', "'"):
        return f'"{s}"'
    return s


def _dump_node(val, indent: int) -> str:
    prefix = "  " * indent
    if isinstance(val, dict):
        if not val:
            return " {}\n"
        parts = ["\n"]
        for k, v in val.items():
            if isinstance(v, (dict, list)):
                parts.append(f"{prefix}{k}:{_dump_node(v, indent + 1)}")
            else:
                parts.append(f"{prefix}{k}: {_scalar_str(v)}\n")
        return "".join(parts)
    if isinstance(val, list):
        if not val:
            return " []\n"
        parts = ["\n"]
        for item in val:
            if isinstance(item, dict):
                first = True
                for k, v in item.items():
                    marker = "- " if first else "  "
                    first = False
                    parts.append(f"{prefix}{marker}{k}: {_scalar_str(v)}\n")
            else:
                parts.append(f"{prefix}- {_scalar_str(item)}\n")
        return "".join(parts)
    return f" {_scalar_str(val)}\n"


def _loads_fallback(text: str) -> dict:
    if not text.strip():
        return {}
    result, _ = _parse_mapping(text.splitlines(), 0, 0)
    return result


def _dumps_fallback(data: dict) -> str:
    parts = []
    for k, v in data.items():
        if isinstance(v, (dict, list)):
            parts.append(f"{k}:{_dump_node(v, 1)}")
        else:
            parts.append(f"{k}: {_scalar_str(v)}\n")
    return "".join(parts)


# --- Config I/O ---


def get_config_path() -> str:
    env_path = os.environ.get("GH_DASH_CONFIG")
    if env_path:
        return env_path
    user_profile = os.environ.get("USERPROFILE", "")
    return os.path.join(user_profile, ".config", "gh-dash", "config.yml")


def log(message: str) -> None:
    sys.stderr.write(f"[gh-dash-plugin] {message}\n")
    sys.stderr.flush()


def read_yaml(file_path: str) -> dict:
    if not os.path.exists(file_path):
        return {}
    with open(file_path, "r", encoding="utf-8") as fh:
        text = fh.read()
    if _HAS_PYYAML:
        data = _yaml.safe_load(text)
        return data if isinstance(data, dict) else {}
    return _loads_fallback(text)


def write_yaml(file_path: str, data: dict) -> None:
    parent = os.path.dirname(file_path)
    if parent:
        os.makedirs(parent, exist_ok=True)
    text = _yaml.dump(data, default_flow_style=False, sort_keys=False) if _HAS_PYYAML else _dumps_fallback(data)
    tmp = file_path + ".tmp"
    try:
        with open(tmp, "w", encoding="utf-8") as fh:
            fh.write(text)
        os.replace(tmp, file_path)
    except Exception:
        if os.path.exists(tmp):
            os.remove(tmp)
        raise


def merge_settings(target: dict, source: dict) -> bool:
    """Merge source into target. Lists are replaced entirely; dicts are merged recursively."""
    changed = False
    for key, value in source.items():
        if isinstance(value, list):
            if target.get(key) != value:
                target[key] = value
                changed = True
        elif isinstance(value, dict):
            if not isinstance(target.get(key), dict):
                target[key] = {}
                changed = True
            if merge_settings(target[key], value):
                changed = True
        else:
            if target.get(key) != value:
                target[key] = value
                changed = True
    return changed


def get_settings_from_args(args: dict) -> dict:
    """Support both wrapped { settings: {...} } and flat { key: value } arg formats."""
    settings = args.get("settings")
    if isinstance(settings, dict):
        return settings
    return args


def check_installed(request_id: str) -> dict:
    if shutil.which("gh-dash") is not None or shutil.which("gh-dash.exe") is not None:
        return {
            "requestId": request_id,
            "success": True,
            "changed": False,
            "data": True,
        }

    try:
        result = subprocess.run(
            ["gh", "ext", "list"],
            capture_output=True,
            text=True,
            timeout=5,
        )
        if "dlvhdr/gh-dash" in result.stdout:
            return {
                "requestId": request_id,
                "success": True,
                "changed": False,
                "data": True,
            }
    except Exception:
        pass

    return {
        "requestId": request_id,
        "success": True,
        "changed": False,
        "data": False,
    }


def apply_config(request_id: str, args: dict, context: dict) -> dict:
    dry_run = bool(context.get("dryRun", False))
    settings = get_settings_from_args(args)

    config_path = get_config_path()
    current_config = read_yaml(config_path)
    changed = merge_settings(current_config, settings)

    if dry_run:
        log(f"dry_run: {'would update' if changed else 'no changes for'} {config_path}")
        return {"requestId": request_id, "success": True, "changed": changed}

    if changed:
        write_yaml(config_path, current_config)

    return {"requestId": request_id, "success": True, "changed": changed}


def handle(request: dict) -> dict:
    request_id = request.get("requestId", "unknown")
    command = request.get("command")
    args = request.get("args", {})
    context = request.get("context", {})

    if command == "check_installed":
        return check_installed(request_id)
    if command == "apply":
        if not isinstance(args, dict):
            raise ValueError("args must be an object")
        if not isinstance(context, dict):
            raise ValueError("context must be an object")
        return apply_config(request_id, args, context)

    raise ValueError(f"Unknown command: {command}")


def main() -> None:
    raw = sys.stdin.read()
    if not raw:
        return

    try:
        request = json.loads(raw)
        result = handle(request)
    except Exception as error:
        result = {
            "requestId": request.get("requestId", "unknown")
            if "request" in locals() and isinstance(request, dict)
            else "unknown",
            "success": False,
            "changed": False,
            "error": str(error),
        }

    sys.stdout.write(json.dumps(result) + "\n")
    sys.stdout.flush()


if __name__ == "__main__":
    main()
