import json
import shutil
import subprocess
import sys

KNOWN_ENV_KEYS = frozenset(
    {
        "GOPATH",
        "GOROOT",
        "GOOS",
        "GOARCH",
        "GO111MODULE",
        "GOPROXY",
        "GONOSUMCHECK",
        "GONOSUMDB",
        "GOPRIVATE",
    }
)


def log(message: str) -> None:
    sys.stderr.write(f"[go-plugin] {message}\n")
    sys.stderr.flush()


def find_go_executable() -> str | None:
    return shutil.which("go.exe") or shutil.which("go")


def normalize_go_value(raw: str) -> str:
    value = raw.strip()
    if len(value) >= 2 and value[0] == value[-1] and value[0] in ('"', "'"):
        return value[1:-1]
    return value


def run_go_env(go_executable: str, keys: list[str] | None = None) -> dict[str, str]:
    command = [go_executable, "env"]
    if keys:
        command.extend(keys)

    result = subprocess.run(
        command,
        capture_output=True,
        text=True,
        check=False,
    )

    if result.returncode != 0:
        stderr = (result.stderr or result.stdout or "go env failed").strip()
        raise RuntimeError(stderr)

    output = (result.stdout or "").strip()
    if not keys:
        env_map: dict[str, str] = {}
        for line in output.splitlines():
            if "=" not in line:
                continue
            key, value = line.split("=", 1)
            env_map[key.strip()] = normalize_go_value(value)
        return env_map

    values = output.splitlines()
    if len(values) != len(keys):
        raise RuntimeError(
            f"Unexpected go env output: expected {len(keys)} values, got {len(values)}"
        )

    return {
        key: normalize_go_value(value)
        for key, value in zip(keys, values, strict=True)
    }


def read_go_env_value(go_executable: str, key: str) -> str:
    values = run_go_env(go_executable, [key])
    return values.get(key, "")


def write_go_env_value(
    go_executable: str, key: str, value: str, dry_run: bool
) -> None:
    assignment = f"{key}={value}"
    if dry_run:
        log(f"Would run: go env -w {assignment}")
        return

    result = subprocess.run(
        [go_executable, "env", "-w", assignment],
        capture_output=True,
        text=True,
        check=False,
    )
    if result.returncode != 0:
        stderr = (result.stderr or result.stdout or "go env -w failed").strip()
        raise RuntimeError(stderr)


def stringify_setting(value) -> str:
    if isinstance(value, bool):
        return "true" if value else "false"
    return str(value)


def check_installed(_args: dict, request_id: str) -> dict:
    installed = find_go_executable() is not None
    return {
        "requestId": request_id,
        "success": True,
        "changed": False,
        "data": installed,
    }


def apply_config(args: dict, context: dict, request_id: str) -> dict:
    dry_run = bool(context.get("dryRun", False))
    settings = args.get("settings", {})

    if not isinstance(settings, dict):
        return {
            "requestId": request_id,
            "success": False,
            "changed": False,
            "error": "settings must be an object",
        }

    go_executable = find_go_executable()
    if not go_executable:
        return {
            "requestId": request_id,
            "success": False,
            "changed": False,
            "error": "Go is not installed or not available on PATH",
        }

    if not settings:
        return {
            "requestId": request_id,
            "success": True,
            "changed": False,
        }

    changed = False
    pending: list[tuple[str, str]] = []

    for key, value in settings.items():
        if value is None:
            continue

        if key not in KNOWN_ENV_KEYS:
            log(f"Skipping unsupported Go env key: {key}")
            continue

        desired = stringify_setting(value)
        current = read_go_env_value(go_executable, key)

        if current != desired:
            changed = True
            pending.append((key, desired))

    if not changed:
        return {
            "requestId": request_id,
            "success": True,
            "changed": False,
        }

    if dry_run:
        for key, desired in pending:
            log(f"Would set {key}={desired}")
        return {
            "requestId": request_id,
            "success": True,
            "changed": True,
        }

    try:
        for key, desired in pending:
            write_go_env_value(go_executable, key, desired, dry_run=False)
            log(f"Set {key}={desired}")
    except Exception as error:
        log(f"Failed to apply Go environment settings: {error}")
        return {
            "requestId": request_id,
            "success": False,
            "changed": False,
            "error": str(error),
        }

    return {
        "requestId": request_id,
        "success": True,
        "changed": True,
    }


def handle(request: dict) -> dict:
    request_id = request.get("requestId", "unknown")
    command = request.get("command")
    args = request.get("args", {})
    context = request.get("context", {})

    if command == "check_installed":
        return check_installed(args, request_id)
    if command == "apply":
        if not isinstance(args, dict):
            return {
                "requestId": request_id,
                "success": False,
                "changed": False,
                "error": "args must be an object",
            }
        if not isinstance(context, dict):
            return {
                "requestId": request_id,
                "success": False,
                "changed": False,
                "error": "context must be an object",
            }
        return apply_config(args, context, request_id)

    return {
        "requestId": request_id,
        "success": False,
        "changed": False,
        "error": f"Unknown command: {command}",
    }


def main() -> None:
    raw = sys.stdin.read()
    if not raw or not raw.strip():
        sys.stdout.write(
            json.dumps(
                {
                    "requestId": "unknown",
                    "success": False,
                    "changed": False,
                    "error": "Empty input",
                }
            )
            + "\n"
        )
        sys.stdout.flush()
        return

    try:
        request = json.loads(raw)
        result = handle(request)
    except json.JSONDecodeError as error:
        result = {
            "requestId": "unknown",
            "success": False,
            "changed": False,
            "error": f"Failed to parse request: {error}",
        }
    except Exception as error:
        request_id = "unknown"
        if "request" in locals() and isinstance(request, dict):
            request_id = request.get("requestId", "unknown")
        result = {
            "requestId": request_id,
            "success": False,
            "changed": False,
            "error": str(error),
        }

    sys.stdout.write(json.dumps(result) + "\n")
    sys.stdout.flush()


if __name__ == "__main__":
    main()
