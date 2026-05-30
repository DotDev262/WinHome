import os
import json
import tempfile
import sys
import shutil

# ---------------- RESPONSE ----------------

def make_response(request_id, success, changed, data=None, error=None):
    response = {
        "requestId": request_id,
        "success": success,
        "changed": changed,
        "data": data if data is not None else {}
    }
    if error:
        response["error"] = error
    return response


# ---------------- CONFIG PATH ----------------

def get_config_path():
    possible_paths = [
        os.path.expandvars(r"%PROGRAMFILES(X86)%\Steam\steamapps\common\wallpaper_engine\config\config.json"),
        os.path.expandvars(r"%PROGRAMFILES%\Steam\steamapps\common\wallpaper_engine\config\config.json"),
        os.path.expanduser(r"~/Steam/steamapps/common/wallpaper_engine/config/config.json")
    ]

    for path in possible_paths:
        if os.path.exists(path):
            return path

    return None


def read_config():
    path = get_config_path()
    if not path:
        return None

    with open(path, "r", encoding="utf-8") as f:
        return json.load(f)


def save_config(config):
    path = get_config_path()
    if not path:
        raise Exception("Wallpaper Engine config not found")

    directory = os.path.dirname(path)

    # backup (required by reviewer)
    backup_path = path + ".backup"
    if os.path.exists(path):
        shutil.copy2(path, backup_path)

    fd, temp_path = tempfile.mkstemp(dir=directory)

    try:
        with os.fdopen(fd, "w", encoding="utf-8") as f:
            json.dump(config, f, indent=4)
            f.write("\n")

        os.replace(temp_path, path)

    finally:
        if os.path.exists(temp_path):
            os.remove(temp_path)


# ---------------- LOGIC ----------------

def deep_merge(base, updates):
    for k, v in updates.items():
        if isinstance(base.get(k), dict) and isinstance(v, dict):
            deep_merge(base[k], v)
        else:
            base[k] = v
    return base


def check_installed(params):
    request_id = params.get("requestId")
    return make_response(
        request_id,
        True,
        False,
        {
            "installed": get_config_path() is not None
        }
    )


def apply(params):
    request_id = params.get("requestId")

    settings = params.get("settings", {})
    context = params.get("context", {})
    dry_run = context.get("dryRun", False)

    # validation
    if not isinstance(settings, dict):
        return make_response(
            request_id,
            False,
            False,
            {},
            "settings must be a dictionary"
        )

    config = read_config()
    if config is None:
        return make_response(
            request_id,
            False,
            False,
            {},
            "Wallpaper Engine config not found"
        )

    original = json.loads(json.dumps(config))  # deep copy

    config = deep_merge(config, settings)
    changed = (config != original)

    if not dry_run:
        save_config(config)

    return make_response(
        request_id,
        True,
        changed,
        {
            "settings": config,
            "dryRun": dry_run
        }
    )


# ---------------- DISPATCH ----------------

def dispatch(request):
    method = request.get("method")
    params = request.get("params", {})

    if method == "check_installed":
        return check_installed(request)

    if method == "apply":
        return apply(request)

    return make_response(
        request.get("requestId"),
        False,
        False,
        {},
        f"Unknown method: {method}"
    )


# ---------------- MAIN (REVIEWER REQUIRED) ----------------

def main():
    raw = sys.stdin.read()

    if not raw:
        sys.stdout.write(json.dumps(
            make_response("unknown", False, False, {}, "Empty stdin")
        ) + "\n")
        sys.stdout.flush()
        return

    try:
        request = json.loads(raw)
        response = dispatch(request)

    except Exception as e:
        response = make_response(
            "unknown",
            False,
            False,
            {},
            str(e)
        )

    sys.stdout.write(json.dumps(response) + "\n")
    sys.stdout.flush()


if __name__ == "__main__":
    main()