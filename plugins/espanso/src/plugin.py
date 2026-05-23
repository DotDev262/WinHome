"""
Espanso plugin for WinHome.

Manages Espanso text expander configuration on Windows.
Config location: %APPDATA%\\espanso\\match\\base.yml

Commands:
  - check_installed: Verify Espanso is installed
  - apply:           Deep-merge matches/global_vars into base.yml
"""

import json
import logging
import os
import sys
from copy import deepcopy
from pathlib import Path

try:
    import yaml
except ImportError:
    print(
        json.dumps({"error": "PyYAML is not installed. Run: pip install PyYAML"}),
        file=sys.stderr,
    )
    sys.exit(1)

logging.basicConfig(
    level=logging.DEBUG,
    format="%(asctime)s [%(levelname)s] %(message)s",
    stream=sys.stderr,
)
logger = logging.getLogger(__name__)


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

def get_base_yml_path() -> Path:
    """Return the canonical path to Espanso's base.yml on Windows."""
    appdata = os.environ.get("APPDATA", "")
    if not appdata:
        raise EnvironmentError("APPDATA environment variable is not set.")
    return Path(appdata) / "espanso" / "match" / "base.yml"


def is_espanso_installed() -> bool:
    """
    Espanso is considered installed when its config directory exists.
    The directory is %APPDATA%\\espanso.
    """
    appdata = os.environ.get("APPDATA", "")
    if not appdata:
        return False
    espanso_dir = Path(appdata) / "espanso"
    return espanso_dir.exists()


def read_yaml(path: Path) -> dict:
    """Read a YAML file and return its contents as a dict (empty dict if missing)."""
    if not path.exists():
        logger.debug("File not found, returning empty config: %s", path)
        return {}
    with path.open("r", encoding="utf-8") as fh:
        data = yaml.safe_load(fh) or {}
    logger.debug("Read YAML from %s: %s", path, data)
    return data


def write_yaml(path: Path, data: dict) -> None:
    """Write *data* as YAML to *path*, creating parent directories if needed."""
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", encoding="utf-8") as fh:
        yaml.dump(data, fh, allow_unicode=True, default_flow_style=False, sort_keys=False)
    logger.debug("Wrote YAML to %s", path)


def deep_merge_lists(existing: list, incoming: list, key: str = "trigger") -> list:
    """
    Merge two lists of dicts by *key*.

    Items in *incoming* that share a key value with an item in *existing*
    replace that item; items with new key values are appended.
    Items in *existing* that have no match in *incoming* are preserved.
    """
    merged = deepcopy(existing)
    existing_index = {item.get(key): idx for idx, item in enumerate(merged) if isinstance(item, dict)}

    for item in incoming:
        if not isinstance(item, dict):
            continue
        item_key = item.get(key)
        if item_key is not None and item_key in existing_index:
            merged[existing_index[item_key]] = deepcopy(item)
            logger.debug("Replaced existing entry with key '%s'", item_key)
        else:
            merged.append(deepcopy(item))
            logger.debug("Appended new entry with key '%s'", item_key)

    return merged


def merge_config(existing: dict, incoming: dict) -> tuple[dict, bool]:
    """
    Deep-merge *incoming* into *existing*.

    Returns (merged_config, changed) where *changed* is True when the
    result differs from *existing*.
    """
    merged = deepcopy(existing)
    changed = False

    # Merge matches list
    if "matches" in incoming:
        old_matches = merged.get("matches", [])
        new_matches = deep_merge_lists(old_matches, incoming["matches"], key="trigger")
        if new_matches != old_matches:
            merged["matches"] = new_matches
            changed = True
            logger.debug("matches changed")

    # Merge global_vars list
    if "global_vars" in incoming:
        old_vars = merged.get("global_vars", [])
        new_vars = deep_merge_lists(old_vars, incoming["global_vars"], key="name")
        if new_vars != old_vars:
            merged["global_vars"] = new_vars
            changed = True
            logger.debug("global_vars changed")

    return merged, changed


# ---------------------------------------------------------------------------
# Command handlers
# ---------------------------------------------------------------------------

def handle_check_installed(_args: dict) -> dict:
    """Return whether Espanso appears to be installed."""
    installed = is_espanso_installed()
    logger.info("check_installed → installed=%s", installed)
    return {"installed": installed}


def handle_apply(args: dict, dry_run: bool = False) -> dict:
    """
    Apply matches / global_vars from *args* into base.yml.

    In dry-run mode the file is never written; the call still returns
    what *would* have changed.
    """
    base_path = get_base_yml_path()
    existing = read_yaml(base_path)
    merged, changed = merge_config(existing, args)

    if dry_run:
        logger.info("[dry-run] apply → changed=%s  (file NOT written)", changed)
        if changed:
            logger.info("[dry-run] would write:\n%s", yaml.dump(merged, allow_unicode=True))
    else:
        if changed:
            write_yaml(base_path, merged)
            logger.info("apply → wrote updated config to %s", base_path)
        else:
            logger.info("apply → no changes, file left untouched")

    return {"success": True, "changed": changed}


# ---------------------------------------------------------------------------
# JSON-over-stdio protocol
# ---------------------------------------------------------------------------

def read_message() -> dict:
    """Read one newline-terminated JSON message from stdin."""
    line = sys.stdin.readline()
    if not line:
        raise EOFError("stdin closed")
    return json.loads(line.strip())


def send_message(payload: dict) -> None:
    """Write one JSON message to stdout and flush immediately."""
    sys.stdout.write(json.dumps(payload) + "\n")
    sys.stdout.flush()


def main() -> None:
    logger.info("Espanso plugin started (PID=%d)", os.getpid())

    while True:
        try:
            msg = read_message()
        except EOFError:
            logger.info("stdin closed, exiting")
            break
        except json.JSONDecodeError as exc:
            logger.error("JSON decode error: %s", exc)
            send_message({"error": f"Invalid JSON: {exc}"})
            continue

        command = msg.get("command", "")
        args = msg.get("args", {})
        dry_run = bool(msg.get("dry_run", False))

        logger.debug("Received command='%s' args=%s dry_run=%s", command, args, dry_run)

        try:
            if command == "check_installed":
                result = handle_check_installed(args)
                send_message({"data": result})

            elif command == "apply":
                result = handle_apply(args, dry_run=dry_run)
                send_message(result)

            else:
                error_msg = f"Unknown command: '{command}'"
                logger.warning(error_msg)
                send_message({"error": error_msg})

        except Exception as exc:  # pylint: disable=broad-except
            logger.exception("Unhandled exception processing command '%s'", command)
            send_message({"error": str(exc)})


if __name__ == "__main__":
    main()