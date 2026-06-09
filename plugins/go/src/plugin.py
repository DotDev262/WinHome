import json
import shutil
import subprocess
import sys

SUPPORTED_KEYS = {
    "GO111MODULE",
    "GOARCH",
    "GONOSUMCHECK",
    "GONOSUMDB",
    "GOOS",
    "GOPATH",
    "GOPRIVATE",
    "GOPROXY",
    "GOROOT",
}


def log(message):
    sys.stderr.write(f"[go-plugin] {message}\n")
    sys.stderr.flush()


def go_executable():
    return shutil.which("go.exe") or shutil.which("go")


def check_installed(args, request_id):
    return {
        "requestId": request_id,
        "success": True,
        "changed": False,
        "data": go_executable() is not None,
    }


def read_go_env(go_bin, key):
    result = subprocess.run(
        [go_bin, "env", key],
        capture_output=True,
        check=False,
        text=True,
    )
    if result.returncode != 0:
        error = result.stderr.strip() or f"go env {key} failed with exit code {result.returncode}"
        raise RuntimeError(error)
    return result.stdout.rstrip("\r\n")


def write_go_env(go_bin, key, value):
    result = subprocess.run(
        [go_bin, "env", "-w", f"{key}={value}"],
        capture_output=True,
        check=False,
        text=True,
    )
    if result.returncode != 0:
        error = result.stderr.strip() or f"go env -w {key}=... failed with exit code {result.returncode}"
        raise RuntimeError(error)


def normalize_settings(raw_settings):
    if raw_settings is None:
        return {}
    if not isinstance(raw_settings, dict):
        raise ValueError("settings must be an object")

    normalized = {}
    for key, value in raw_settings.items():
        if key not in SUPPORTED_KEYS:
            raise ValueError(f"Unsupported Go environment setting: {key}")
        if value is None:
            raise ValueError(f"{key} cannot be null")
        normalized[key] = str(value)
    return normalized


def planned_changes(go_bin, settings):
    changes = {}
    current_values = {}
    for key, desired in settings.items():
        current = read_go_env(go_bin, key)
        current_values[key] = current
        if current != desired:
            changes[key] = {
                "current": current,
                "desired": desired,
            }
    return changes, current_values


def apply_config(args, context, request_id):
    dry_run = bool(context.get("dryRun", False))

    try:
        settings = normalize_settings(args.get("settings", {}))
        go_bin = go_executable()
        if go_bin is None:
            return {
                "requestId": request_id,
                "success": False,
                "changed": False,
                "error": "Go executable not found in PATH",
            }

        changes, current_values = planned_changes(go_bin, settings)
        if not changes:
            return {
                "requestId": request_id,
                "success": True,
                "changed": False,
                "data": {
                    "current": current_values,
                    "planned": {},
                },
            }

        if dry_run:
            log(f"dry_run: would update Go environment settings: {json.dumps(changes, sort_keys=True)}")
            return {
                "requestId": request_id,
                "success": True,
                "changed": True,
                "data": {
                    "planned": changes,
                },
            }

        for key, change in changes.items():
            write_go_env(go_bin, key, change["desired"])

        log(f"Updated {len(changes)} Go environment setting(s)")
        return {
            "requestId": request_id,
            "success": True,
            "changed": True,
            "data": {
                "applied": changes,
            },
        }
    except Exception as error:
        log(f"Failed to apply Go environment settings: {error}")
        return {
            "requestId": request_id,
            "success": False,
            "changed": False,
            "error": str(error),
        }


def handle(request):
    request_id = request.get("requestId", "unknown")
    command = request.get("command")
    args = request.get("args", {})
    context = request.get("context", {})

    if not isinstance(args, dict):
        raise ValueError("args must be an object")
    if not isinstance(context, dict):
        raise ValueError("context must be an object")

    if command == "check_installed":
        return check_installed(args, request_id)
    if command == "apply":
        return apply_config(args, context, request_id)

    return {
        "requestId": request_id,
        "success": False,
        "changed": False,
        "error": f"Unknown command: {command}",
    }


def error_response(request_id, error):
    return {
        "requestId": request_id,
        "success": False,
        "changed": False,
        "error": str(error),
    }


def main():
    input_data = sys.stdin.read()
    if not input_data:
        sys.stdout.write(json.dumps(error_response("unknown", "No input provided")) + "\n")
        sys.stdout.flush()
        return

    request = {}
    try:
        request = json.loads(input_data)
        if not isinstance(request, dict):
            raise ValueError("request must be an object")
        response = handle(request)
    except Exception as error:
        request_id = request.get("requestId", "unknown") if isinstance(request, dict) else "unknown"
        response = error_response(request_id, error)

    sys.stdout.write(json.dumps(response) + "\n")
    sys.stdout.flush()


if __name__ == "__main__":
    main()
