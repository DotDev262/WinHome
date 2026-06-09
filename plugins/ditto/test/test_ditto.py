import json
import os
import subprocess
import sys
import tempfile

PLUGIN = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "src", "plugin.py"))


def run_plugin(payload: dict, env: dict = None) -> dict:
    run_env = os.environ.copy()
    if env:
        run_env.update(env)
    result = subprocess.run(
        [sys.executable, PLUGIN],
        input=json.dumps(payload),
        capture_output=True,
        text=True,
        env=run_env,
    )
    return json.loads(result.stdout.strip())


def test_check_installed_false():
    with tempfile.TemporaryDirectory() as tmp:
        res = run_plugin(
            {
                "requestId": "1",
                "command": "check_installed",
                "args": {},
                "context": {},
            },
            env={"APPDATA": tmp, "PATH": ""},
        )
        assert "error" not in res
        assert res["installed"] == False
        print("✓ check_installed_false")


def test_check_installed_true():
    with tempfile.TemporaryDirectory() as tmp:
        ditto_dir = os.path.join(tmp, "Ditto")
        os.makedirs(ditto_dir)
        open(os.path.join(ditto_dir, "Ditto.exe"), "w").close()

        res = run_plugin(
            {
                "requestId": "2",
                "command": "check_installed",
                "args": {},
                "context": {},
            },
            env={"APPDATA": tmp, "PATH": ""},
        )
        assert "error" not in res
        assert res["installed"] == True
        print("✓ check_installed_true")


def test_apply_settings():
    with tempfile.TemporaryDirectory() as tmp:
        config_path = os.path.join(tmp, "Ditto", "Ditto.settings")

        res = run_plugin(
            {
                "requestId": "3",
                "command": "apply",
                "args": {
                    "settings": {
                        "max_clipboards": 500,
                        "play_sound": True,
                        "show_tray_icon": True,
                    },
                    "dryRun": False,
                },
                "context": {},
            },
            env={"APPDATA": tmp},
        )

        assert "error" not in res
        assert res["changed"]

        saved = json.loads(open(config_path).read())
        assert saved["max_clipboards"] == 500
        assert saved["play_sound"] == True
        assert saved["show_tray_icon"] == True
        print("✓ apply_settings")


def test_idempotent():
    with tempfile.TemporaryDirectory() as tmp:
        payload = {
            "requestId": "4",
            "command": "apply",
            "args": {"settings": {"max_clipboards": 100}, "dryRun": False},
            "context": {},
        }
        run_plugin(payload, env={"APPDATA": tmp})
        res = run_plugin(payload, env={"APPDATA": tmp})
        assert "error" not in res
        assert not res["changed"]
        print("✓ idempotent")


def test_dry_run():
    with tempfile.TemporaryDirectory() as tmp:
        config_path = os.path.join(tmp, "Ditto", "Ditto.settings")

        res = run_plugin(
            {
                "requestId": "5",
                "command": "apply",
                "args": {"settings": {"max_clipboards": 200}, "dryRun": True},
                "context": {},
            },
            env={"APPDATA": tmp},
        )
        assert "error" not in res
        assert res["changed"]
        assert not os.path.exists(config_path)
        print("✓ dry_run")


def test_unknown_command():
    res = run_plugin({"requestId": "6", "command": "explode", "args": {}, "context": {}})
    assert "error" in res
    assert "success" not in res
    print("✓ unknown_command")


if __name__ == "__main__":
    test_check_installed_false()
    test_check_installed_true()
    test_apply_settings()
    test_idempotent()
    test_dry_run()
    test_unknown_command()
    print("\nAll tests passed.")
