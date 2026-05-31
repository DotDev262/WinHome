"""
Espanso plugin for WinHome.

Manages Espanso text expander configuration on Windows.
Config location: %APPDATA%\\espanso\\match\\base.yml

Commands:
  - check_installed: Verify Espanso is installed
  - apply:           Deep-merge matches/global_vars into base.yml
"""

import json
import os
import sys
from copy import deepcopy
from pathlib import Path

# ---------------------------------------------------------------------------
# Logging — simple stderr helper, matching other WinHome plugins
# ---------------------------------------------------------------------------


def log(msg: str) -> None:
    print(f"[espanso] {msg}", file=sys.stderr, flush=True)


# ---------------------------------------------------------------------------
# YAML helpers (stdlib-only, no PyYAML dependency)
# ---------------------------------------------------------------------------


def _yaml_value(v) -> str:
    """Serialise a scalar Python value to a YAML-safe string."""
    if isinstance(v, bool):
        return "true" if v else "false"
    if isinstance(v, (int, float)):
        return str(v)
    s = str(v)
    if any(
        c in s
        for c in (
            ":",
            "#",
            "[",
            "]",
            "{",
            "}",
            ",",
            "&",
            "*",
            "?",
            "|",
            "-",
            "<",
            ">",
            "=",
            "!",
            "%",
            "@",
            "`",
            '"',
            "'",
        )
    ):
        escaped = s.replace('"', '\\"')
        return f'"{escaped}"'
    if s in ("true", "false", "null", "yes", "no", "on", "off", ""):
        return f'"{s}"'
    return s


def _dump_yaml(data: dict, indent: int = 0) -> str:
    """
    Minimal recursive YAML serialiser.

    Handles the subset used by Espanso base.yml:
    top-level keys, lists of dicts, and scalar values.
    """
    lines = []
    pad = "  " * indent
    for key, value in data.items():
        if isinstance(value, list):
            lines.append(f"{pad}{key}:")
            for item in value:
                if isinstance(item, dict):
                    first = True
                    for k, v in item.items():
                        if isinstance(v, dict):
                            if first:
                                lines.append(f"{pad}  - {k}:")
                            else:
                                lines.append(f"{pad}    {k}:")
                            for ik, iv in v.items():
                                lines.append(f"{pad}      {ik}: {_yaml_value(iv)}")
                        else:
                            prefix = f"{pad}  - " if first else f"{pad}    "
                            lines.append(f"{prefix}{k}: {_yaml_value(v)}")
                        first = False
                else:
                    lines.append(f"{pad}  - {_yaml_value(item)}")
        elif isinstance(value, dict):
            lines.append(f"{pad}{key}:")
            lines.append(_dump_yaml(value, indent + 1))
        else:
            lines.append(f"{pad}{key}: {_yaml_value(value)}")
    return "\n".join(lines)


def _parse_yaml(text: str) -> dict:
    """
    Minimal YAML parser for Espanso base.yml.

    Handles top-level keys, lists of dicts, and scalar values.
    Falls back gracefully to an empty dict on parse errors.
    """
    try:
        import yaml  # noqa: PLC0415

        return yaml.safe_load(text) or {}
    except ImportError:
        pass

    # Stdlib fallback — parses the specific structure Espanso uses
    result: dict = {}
    current_list: list | None = None
    current_item: dict | None = None

    for raw_line in text.splitlines():
        line = raw_line.rstrip()
        if not line or line.lstrip().startswith("#"):
            continue

        stripped = line.lstrip()
        depth = len(line) - len(stripped)

        # Top-level key (depth 0): "key:" or "key: value"
        if depth == 0 and not stripped.startswith("-"):
            if ":" in stripped:
                k, _, v = stripped.partition(":")
                k = k.strip()
                v = v.strip()
                if v:
                    result[k] = _cast(v)
                    current_list = None
                    current_item = None
                else:
                    current_list = []
                    result[k] = current_list
                    current_item = None
            continue

        # List item opener: "  - key: value"
        if stripped.startswith("- ") and current_list is not None:
            kv = stripped[2:].strip()
            current_item = {}
            current_list.append(current_item)
            if ":" in kv:
                k, _, v = kv.partition(":")
                k = k.strip()
                v = v.strip()
                if v:
                    current_item[k] = _cast(v)
                else:
                    current_item[k] = {}
            continue

        # Continuation key under a list item: "    key: value"
        if current_item is not None and ":" in stripped and not stripped.startswith("-"):
            k, _, v = stripped.partition(":")
            k = k.strip()
            v = v.strip()
            if not v:
                current_item[k] = {}
            else:
                last_val = list(current_item.values())[-1] if current_item else None
                if isinstance(last_val, dict) and depth >= 6:
                    last_val[k] = _cast(v)
                else:
                    current_item[k] = _cast(v)
            continue

    return result


def _cast(v: str):
    """Cast a YAML scalar string to the appropriate Python type."""
    v = v.strip().strip('"').strip("'")
    if v.lower() == "true":
        return True
    if v.lower() == "false":
        return False
    if v.lower() == "null":
        return None
    try:
        return int(v)
    except ValueError:
        pass
    try:
        return float(v)
    except ValueError:
        pass
    return v


# ---------------------------------------------------------------------------
# File helpers
# ---------------------------------------------------------------------------


def get_base_yml_path() -> Path:
    """Return the canonical path to Espanso's base.yml on Windows."""
    appdata = os.environ.get("APPDATA", "")
    if not appdata:
        raise EnvironmentError("APPDATA environment variable is not set.")
    return Path(appdata) / "espanso" / "match" / "base.yml"


def is_espanso_installed() -> bool:
    """Espanso is considered installed when its config directory exists."""
    appdata = os.environ.get("APPDATA", "")
    if not appdata:
        return False
    return (Path(appdata) / "espanso").exists()


def read_config(path: Path) -> dict:
    """Read a YAML config file; return empty dict if missing or unreadable."""
    if not path.exists():
        log(f"config not found, starting empty: {path}")
        return {}
    text = path.read_text(encoding="utf-8")
    return _parse_yaml(text)


def write_config(path: Path, data: dict) -> None:
    """Write *data* as YAML to *path*, creating parent directories if needed."""
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(_dump_yaml(data) + "\n", encoding="utf-8")
    log(f"wrote config to {path}")


# ---------------------------------------------------------------------------
# Merge logic
# ---------------------------------------------------------------------------


def deep_merge_lists(existing: list, incoming: list, key: str = "trigger") -> list:
    """
    Merge two lists of dicts by *key*.

    Incoming items replace existing items with the same key value.
    Existing items not present in incoming are preserved.
    New items in incoming are appended.
    """
    merged = deepcopy(existing)
    index = {item.get(key): i for i, item in enumerate(merged) if isinstance(item, dict)}
    for item in incoming:
        if not isinstance(item, dict):
            continue
        k = item.get(key)
        if k is not None and k in index:
            merged[index[k]] = deepcopy(item)
        else:
            merged.append(deepcopy(item))
    return merged


def merge_config(existing: dict, incoming: dict) -> tuple[dict, bool]:
    """
    Deep-merge *incoming* into *existing*.

    Returns (merged_config, changed).
    """
    merged = deepcopy(existing)
    changed = False

    if "matches" in incoming:
        old = merged.get("matches", [])
        new = deep_merge_lists(old, incoming["matches"], key="trigger")
        if new != old:
            merged["matches"] = new
            changed = True

    if "global_vars" in incoming:
        old = merged.get("global_vars", [])
        new = deep_merge_lists(old, incoming["global_vars"], key="name")
        if new != old:
            merged["global_vars"] = new
            changed = True

    return merged, changed


# ---------------------------------------------------------------------------
# Command handlers
# ---------------------------------------------------------------------------


def handle_check_installed(request_id: str, _args: dict) -> dict:
    installed = is_espanso_installed()
    log(f"check_installed → installed={installed}")
    return {
        "requestId": request_id,
        "success": True,
        "changed": False,
        "data": {"installed": installed},
    }


def handle_apply(request_id: str, args: dict, dry_run: bool = False) -> dict:
    base_path = get_base_yml_path()
    existing = read_config(base_path)
    merged, changed = merge_config(existing, args)

    if dry_run:
        log(f"[dry-run] apply → changed={changed} (file NOT written)")
    else:
        if changed:
            write_config(base_path, merged)
        else:
            log("apply → no changes, file left untouched")

    return {
        "requestId": request_id,
        "success": True,
        "changed": changed,
    }


# ---------------------------------------------------------------------------
# Single-shot JSON-over-stdio protocol
# ---------------------------------------------------------------------------


def main() -> None:
    try:
        msg = json.loads(sys.stdin.read())
    except json.JSONDecodeError as exc:
        sys.stdout.write(
            json.dumps(
                {
                    "requestId": None,
                    "success": False,
                    "changed": False,
                    "error": f"Invalid JSON: {exc}",
                }
            )
            + "\n"
        )
        sys.stdout.flush()
        return

    request_id = msg.get("requestId", "")
    command = msg.get("command", "")
    args = msg.get("args", {})
    dry_run = bool(msg.get("context", {}).get("dryRun", False))

    log(f"command={command} requestId={request_id} dryRun={dry_run}")

    try:
        if command == "check_installed":
            response = handle_check_installed(request_id, args)
        elif command == "apply":
            response = handle_apply(request_id, args, dry_run=dry_run)
        else:
            response = {
                "requestId": request_id,
                "success": False,
                "changed": False,
                "error": f"unknown command: {command!r}",
            }
    except Exception as exc:  # noqa: BLE001
        log(f"unhandled error: {exc}")
        response = {
            "requestId": request_id,
            "success": False,
            "changed": False,
            "error": str(exc),
        }

    sys.stdout.write(json.dumps(response) + "\n")
    sys.stdout.flush()


if __name__ == "__main__":
    main()
