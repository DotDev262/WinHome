import json
import os
import sys
import tempfile

# from pathlib import Path


SETTING_FILE = "prefs"


def log(msg):
    sys.stderr.write(f"[spotify-plugin] {msg}\n")
    sys.stderr.flush()


def get_config_path():
    appdata = os.getenv("APPDATA")

    if not appdata:
        raise Exception("APPDATA environment variable not found")

    config_dir = os.path.join(appdata, "Spotify")
    os.makedirs(config_dir, exist_ok=True)

    return os.path.join(config_dir, SETTING_FILE)


def read_prefs(file_path: str) -> dict:
    if not os.path.exists(file_path):
        return {}

    prefs = {}

    try:
        with open(file_path, "r", encoding="utf-8") as f:
            for line in f:
                line = line.strip()

                if not line or "=" not in line:
                    continue

                key, value = line.split("=", 1)
                prefs[key] = value

    except Exception as e:
        log(f"Warning: could not parse {file_path}: {e}")

    return prefs


def write_prefs(file_path: str, prefs: dict) -> None:
    os.makedirs(os.path.dirname(file_path), exist_ok=True)

    fd, temp_path = tempfile.mkstemp(
        dir=os.path.dirname(file_path),
        suffix=".tmp",
    )

    try:
        with os.fdopen(fd, "w", encoding="utf-8") as f:
            for key, value in prefs.items():
                f.write(f"{key}={value}\n")

        os.replace(temp_path, file_path)

    except Exception:
        if os.path.exists(temp_path):
            os.remove(temp_path)
        raise


def merge_settings(target: dict, source: dict) -> bool:
    changed = False

    for key, value in source.items():
        value = str(value)

        if key not in target or target[key] != value:
            target[key] = value
            changed = True

    return changed


def check_installed():
    appdata = os.getenv("APPDATA")

    if not appdata:
        return False

    spotify_path = os.path.join(appdata, "Spotify")
    return os.path.exists(spotify_path)


def apply_config(args: dict, request_id: str) -> dict:
    dry_run = args.get("dryRun", False)

    settings = args.get("settings", {})

    if not isinstance(settings, dict):
        return {
            "requestId": request_id,
            "error": "settings must be a dictionary",
        }

    try:
        config_path = get_config_path()

        current_config = read_prefs(config_path)

        changed = merge_settings(current_config, settings)

        if not changed:
            return {
                "requestId": request_id,
                "changed": False,
            }

        if dry_run:
            log(f"Would update {config_path} with: {json.dumps(settings)}")

            return {
                "requestId": request_id,
                "changed": True,
            }

        write_prefs(config_path, current_config)

        log(f"Updated Spotify prefs: {config_path}")

        return {
            "requestId": request_id,
            "changed": True,
        }

    except Exception as e:
        log(f"Failed to apply config: {e}")

        return {
            "requestId": request_id,
            "error": str(e),
        }


def main():
    input_data = sys.stdin.read()

    if not input_data:
        sys.stdout.write(
          json.dumps(
              {
                  "requestId": "unknown",
                  "error": "No input received",
              }
          )
          + "\n"
        )
        sys.stdout.flush()
        return

    try:
        request = json.loads(input_data)
    except Exception as e:
        log(f"Failed to parse request: {e}")
        sys.stdout.write(
            json.dumps(
                {
                  "requestId": "unknown",
                  "error": f"Failed to parse request: {e}",
                }
              )
          + "\n"
        )

        sys.stdout.flush()
        return

    request_id = request.get("requestId") or "unknown"
    command = request.get("command")
    args = request.get("args", {})

    response = {
        "requestId": request_id,
    }

    try:
        if command == "check_installed":
            installed = check_installed()

            response = {
              "requestId": request_id,
              "installed": installed,
            }

        elif command == "apply":
            response = apply_config(args, request_id)

        else:
            response["error"] = f"Unknown command: {command}"

    except Exception as fatal_err:
        response["error"] = f"Internal Script Error: {str(fatal_err)}"

    sys.stdout.write(json.dumps(response) + "\n")
    sys.stdout.flush()


if __name__ == "__main__":
    main()
