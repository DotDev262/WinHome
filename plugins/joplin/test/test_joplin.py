import json
import os
import subprocess
import sys
import tempfile

PLUGIN = os.path.abspath(
    os.path.join(
        os.path.dirname(__file__),
        "..",
        "src",
        "plugin.py",
    )
)


def run_plugin(payload: dict, env: dict | None = None) -> dict:
    merged_env = os.environ.copy()
    if env:
        merged_env.update(env)

    result = subprocess.run(
        [sys.executable, PLUGIN],
        input=json.dumps(payload),
        capture_output=True,
        text=True,
        env=merged_env,
    )

    if result.returncode != 0:
        print(f"Error output: {result.stderr}")

    return json.loads(result.stdout.strip())


def get_settings_path(appdata: str) -> str:
    return os.path.join(appdata, "joplin-desktop", "settings.json")


# --- check_installed ---

def test_check_installed():
    res = run_plugin(
        {
            "requestId": "1",
            "command": "check_installed",
            "args": {},
            "context": {},
        }
    )

    assert res["requestId"] == "1"
    assert res["success"] is True
    assert res["changed"] is False
    assert isinstance(res["data"], bool)
    print("OK: test_check_installed")


# --- apply ---

def test_apply_creates_file():
    with tempfile.TemporaryDirectory() as tmp:
        payload = {
            "requestId": "2",
            "command": "apply",
            "args": {
                "settings": {
                    "editor.codeView": True,
                    "theme": 1,
                }
            },
            "context": {"dryRun": False},
        }
        res = run_plugin(payload, {"APPDATA": tmp})
        assert res["success"] is True
        assert res["changed"] is True
        assert "data" in res

        config_path = get_settings_path(tmp)
        assert os.path.exists(config_path)

        with open(config_path, "r", encoding="utf-8") as f:
            data = json.load(f)

        assert data["editor.codeView"] is True
        assert data["theme"] == 1
        print("OK: test_apply_creates_file")


def test_apply_merges_existing():
    with tempfile.TemporaryDirectory() as tmp:
        config_path = get_settings_path(tmp)
        os.makedirs(os.path.dirname(config_path), exist_ok=True)
        with open(config_path, "w", encoding="utf-8") as f:
            json.dump({"locale": "en-US", "theme": 1}, f)

        payload = {
            "requestId": "3",
            "command": "apply",
            "args": {
                "settings": {
                    "theme": 2,
                    "editor.codeView": True
                }
            },
            "context": {"dryRun": False},
        }
        res = run_plugin(payload, {"APPDATA": tmp})
        assert res["success"] is True
        assert res["changed"] is True
        assert "data" in res

        with open(config_path, "r", encoding="utf-8") as f:
            data = json.load(f)

        assert data["locale"] == "en-US"   # preserved
        assert data["theme"] == 2           # updated
        assert data["editor.codeView"] is True  # added
        print("OK: test_apply_merges_existing")


def test_apply_no_changes():
    with tempfile.TemporaryDirectory() as tmp:
        config_path = get_settings_path(tmp)
        os.makedirs(os.path.dirname(config_path), exist_ok=True)
        with open(config_path, "w", encoding="utf-8") as f:
            json.dump({"theme": 2}, f)

        payload = {
            "requestId": "4",
            "command": "apply",
            "args": {
                "settings": {
                    "theme": 2
                }
            },
            "context": {"dryRun": False},
        }
        res = run_plugin(payload, {"APPDATA": tmp})
        assert res["success"] is True
        assert res["changed"] is False
        assert "data" in res
        print("OK: test_apply_no_changes")


def test_dry_run_does_not_write():
    with tempfile.TemporaryDirectory() as tmp:
        payload = {
            "requestId": "5",
            "command": "apply",
            "args": {"settings": {"theme": 1}},
            "context": {"dryRun": True},
        }
        res = run_plugin(payload, {"APPDATA": tmp})
        assert res["success"] is True
        assert res["changed"] is True   # would-be change is reported
        assert "data" in res
        assert not os.path.exists(get_settings_path(tmp))  # file NOT created
        print("OK: test_dry_run_does_not_write")


# --- error handling ---

def test_missing_appdata():
    payload = {
        "requestId": "6",
        "command": "apply",
        "args": {"settings": {"theme": 1}},
        "context": {"dryRun": False},
    }
    res = run_plugin(payload, {"APPDATA": ""})
    assert res["success"] is False
    assert "data" in res
    assert "APPDATA environment variable not found" in res["error"]
    print("OK: test_missing_appdata")


def test_empty_stdin_returns_json_error():
    result = subprocess.run(
        [sys.executable, PLUGIN],
        input="",
        capture_output=True,
        text=True,
    )
    res = json.loads(result.stdout.strip())
    assert res["success"] is False
    assert "data" in res
    assert "Empty input" in res["error"]
    print("OK: test_empty_stdin_returns_json_error")


def test_invalid_json_returns_json_error():
    result = subprocess.run(
        [sys.executable, PLUGIN],
        input="{not valid json",
        capture_output=True,
        text=True,
    )
    res = json.loads(result.stdout.strip())
    assert res["success"] is False
    assert "data" in res
    assert "Failed to parse request" in res["error"]
    print("OK: test_invalid_json_returns_json_error")


def test_unknown_command_returns_error():
    res = run_plugin({
        "requestId": "7",
        "command": "bogus_command",
        "args": {},
        "context": {},
    })
    assert res["success"] is False
    assert "data" in res
    assert "Unknown command" in res["error"]
    print("OK: test_unknown_command_returns_error")


def test_corruption_backup_and_recovery():
    with tempfile.TemporaryDirectory() as tmp:
        config_path = get_settings_path(tmp)
        os.makedirs(os.path.dirname(config_path), exist_ok=True)

        # Write corrupted JSON
        with open(config_path, "w", encoding="utf-8") as f:
            f.write("{this is not valid json")

        payload = {
            "requestId": "8",
            "command": "apply",
            "args": {"settings": {"theme": 1}},
            "context": {"dryRun": False},
        }
        res = run_plugin(payload, {"APPDATA": tmp})
        assert res["success"] is True
        assert res["changed"] is True

        # A .bak file should have been created
        joplin_dir = os.path.dirname(config_path)
        backups = [f for f in os.listdir(joplin_dir) if f.endswith(".bak")]
        assert len(backups) == 1

        # The settings file should now be valid
        with open(config_path, "r", encoding="utf-8") as f:
            data = json.load(f)
        assert data["theme"] == 1
        print("OK: test_corruption_backup_and_recovery")


if __name__ == "__main__":
    test_check_installed()
    test_apply_creates_file()
    test_apply_merges_existing()
    test_apply_no_changes()
    test_dry_run_does_not_write()
    test_missing_appdata()
    test_empty_stdin_returns_json_error()
    test_invalid_json_returns_json_error()
    test_unknown_command_returns_error()
    test_corruption_backup_and_recovery()
    print("\nAll Joplin tests passed.")
