import json
import os
import subprocess
import sys
import tempfile

PLUGIN = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "src", "plugin.py"))


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
        }
    )

    assert res["requestId"] == "1"
    assert isinstance(res["installed"], bool)
    print("✓ check_installed")


def test_apply_config_dry_run():
    with tempfile.TemporaryDirectory() as tmp:
        os.environ["APPDATA"] = tmp

        res = run_plugin(
            {
                "requestId": "2",
                "command": "apply",
                "args": {
                    "settings": {"enableHardwareAcceleration": False},
                    "dryRun": True,
                },
            }
        )

        assert res["changed"] is True

        print("✓ apply_config_dry_run")


def test_apply_config():
    with tempfile.TemporaryDirectory() as tmp:
        os.environ["APPDATA"] = tmp

        res = run_plugin(
            {
                "requestId": "3",
                "command": "apply",
                "args": {
                    "settings": {
                        "enableHardwareAcceleration": False,
                        "IS_MAXIMIZED": True,
                    }
                },
            }
        )

        assert res["requestId"] == "3"
        assert res["changed"] is True

        config_path = os.path.join(tmp, "discord", "settings.json")

        assert os.path.exists(config_path)

        with open(config_path, "r", encoding="utf-8") as f:
            config = json.load(f)

        assert config["enableHardwareAcceleration"] is False
        assert config["IS_MAXIMIZED"] is True

        print("✓ apply_config")


def test_nested_merge():
    with tempfile.TemporaryDirectory() as tmp:
        os.environ["APPDATA"] = tmp

        discord_dir = os.path.join(tmp, "discord")
        os.makedirs(discord_dir, exist_ok=True)

        config_path = os.path.join(discord_dir, "settings.json")

        with open(config_path, "w", encoding="utf-8") as f:
            json.dump(
                {
                    "WINDOW_BOUNDS": {
                        "x": 0,
                        "y": 0,
                        "width": 1000,
                        "height": 800,
                    }
                },
                f,
            )

        res = run_plugin(
            {
                "requestId": "4",
                "command": "apply",
                "args": {"settings": {"WINDOW_BOUNDS": {"width": 1920}}},
            }
        )

        assert res["changed"] is True

        with open(config_path, "r", encoding="utf-8") as f:
            config = json.load(f)

        assert config["WINDOW_BOUNDS"]["x"] == 0
        assert config["WINDOW_BOUNDS"]["y"] == 0
        assert config["WINDOW_BOUNDS"]["width"] == 1920
        assert config["WINDOW_BOUNDS"]["height"] == 800

        print("✓ nested_merge")


def test_idempotent_apply():
    with tempfile.TemporaryDirectory() as tmp:
        os.environ["APPDATA"] = tmp

        payload = {
            "requestId": "5",
            "command": "apply",
            "args": {"settings": {"enableHardwareAcceleration": False}},
        }

        run_plugin(payload)

        res = run_plugin(payload)

        assert res["changed"] is False

        print("✓ idempotent_apply")


def test_unknown_command():
    res = run_plugin(
        {
            "requestId": "6",
            "command": "explode",
            "args": {},
        }
    )

    assert "error" in res

    print("✓ unknown_command")


def test_empty_stdin_returns_error():
    result = subprocess.run(
        [sys.executable, PLUGIN],
        input="",
        capture_output=True,
        text=True,
    )

    res = json.loads(result.stdout.strip())

    assert res["requestId"] == "unknown"
    assert "error" in res

    print("✓ empty_stdin_returns_error")


def test_invalid_json_returns_error():
    result = subprocess.run(
        [sys.executable, PLUGIN],
        input="{invalid json",
        capture_output=True,
        text=True,
    )

    res = json.loads(result.stdout.strip())

    assert res["requestId"] == "unknown"
    assert "error" in res

    print("✓ invalid_json_returns_error")


if __name__ == "__main__":
    test_check_installed()
    test_apply_config_dry_run()
    test_apply_config()
    test_nested_merge()
    test_idempotent_apply()
    test_unknown_command()
    test_invalid_json_returns_error()
    test_empty_stdin_returns_error()
    print("\nAll tests passed.")
