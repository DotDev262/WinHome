import json
import os
import subprocess
import sys
import tempfile

PLUGIN = os.path.abspath(
    os.path.join(os.path.dirname(__file__), "..", "src", "plugin.py")
)


def run_plugin(payload: dict) -> dict:
    result = subprocess.run(
        [sys.executable, PLUGIN],
        input=json.dumps(payload),
        capture_output=True,
        text=True,
    )

    return json.loads(result.stdout.strip())


def test_check_installed():
    res = run_plugin(
        {
            "requestId": "1",
            "command": "check_installed",
            "args": {},
            "context": {},
        }
    )

    assert "success" in res
    print("✓ check_installed")


def test_apply_config_dry_run():
    with tempfile.TemporaryDirectory() as tmp:
        os.environ["APPDATA"] = tmp

        res = run_plugin(
            {
                "requestId": "2",
                "command": "apply",
                "args": {"settings": {"theme": "DarkMode", "fontSize": 16}},
                "context": {"dryRun": True},
            }
        )

        assert res["success"]
        assert not res["changed"]

        print("✓ apply_config_dry_run")


def test_apply_config():
    with tempfile.TemporaryDirectory() as tmp:
        os.environ["APPDATA"] = tmp

        res = run_plugin(
            {
                "requestId": "3",
                "command": "apply",
                "args": {"settings": {"theme": "DarkMode", "wordWrap": True}},
                "context": {"dryRun": False},
            }
        )

        assert res["success"]
        assert res["changed"]

        config_path = os.path.join(tmp, "Notepad++", "config.json")

        assert os.path.exists(config_path)

        config = json.loads(open(config_path).read())

        assert config["theme"] == "DarkMode"
        assert config["wordWrap"] is True

        print("✓ apply_config")


def test_idempotent_apply():
    with tempfile.TemporaryDirectory() as tmp:
        os.environ["APPDATA"] = tmp

        payload = {
            "requestId": "4",
            "command": "apply",
            "args": {"settings": {"theme": "DarkMode"}},
            "context": {"dryRun": False},
        }

        run_plugin(payload)

        res = run_plugin(payload)

        assert res["success"]
        assert not res["changed"]

        print("✓ idempotent_apply")


def test_unknown_command():
    res = run_plugin(
        {"requestId": "5", "command": "explode", "args": {}, "context": {}}
    )

    assert not res["success"]
    assert "error" in res

    print("✓ unknown_command")


if __name__ == "__main__":
    test_check_installed()
    test_apply_config_dry_run()
    test_apply_config()
    test_idempotent_apply()
    test_unknown_command()

    print("\nAll tests passed.")
