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
                    "settings": {"audio.play_bitrate_enumeration": "5"},
                    "dryRun": True,
                },
            }
        )

        assert res["requestId"] == "2"
        assert res["changed"] is True

        print("✓ apply_config_dry_run")


def test_merge_existing_preferences():
    with tempfile.TemporaryDirectory() as tmp:
        os.environ["APPDATA"] = tmp

        spotify_dir = os.path.join(tmp, "Spotify")
        os.makedirs(spotify_dir, exist_ok=True)

        prefs_path = os.path.join(spotify_dir, "prefs")

        with open(prefs_path, "w", encoding="utf-8") as f:
            f.write("audio.play_bitrate_enumeration=4\nui.track_notifications_enabled=false\n")

        res = run_plugin(
            {
                "requestId": "4",
                "command": "apply",
                "args": {"settings": {"audio.play_bitrate_enumeration": "5"}},
            }
        )

        assert res["requestId"] == "4"
        assert res["changed"] is True

        with open(prefs_path, "r", encoding="utf-8") as f:
            content = f.read()

        assert "audio.play_bitrate_enumeration=5" in content
        assert "ui.track_notifications_enabled=false" in content

        print("✓ merge_existing_preferences")


def test_apply_config():
    with tempfile.TemporaryDirectory() as tmp:
        os.environ["APPDATA"] = tmp

        res = run_plugin(
            {
                "requestId": "3",
                "command": "apply",
                "args": {
                    "settings": {
                        "audio.play_bitrate_enumeration": "5",
                        "ui.track_notifications_enabled": "true",
                    },
                },
            }
        )

        assert res["requestId"] == "3"
        assert res["changed"] is True

        config_path = os.path.join(tmp, "Spotify", "prefs")

        assert os.path.exists(config_path)

        with open(config_path, "r", encoding="utf-8") as f:
            content = f.read()

        assert "audio.play_bitrate_enumeration=5" in content
        assert "ui.track_notifications_enabled=true" in content

        print("✓ apply_config")


def test_idempotent_apply():
    with tempfile.TemporaryDirectory() as tmp:
        os.environ["APPDATA"] = tmp

        payload = {
            "requestId": "5",
            "command": "apply",
            "args": {"settings": {"audio.play_bitrate_enumeration": "5"}},
        }

        run_plugin(payload)

        res = run_plugin(payload)

        assert res["requestId"] == "5"
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

    assert res["requestId"] == "6"
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
    test_merge_existing_preferences()
    test_idempotent_apply()
    test_unknown_command()
    test_empty_stdin_returns_error()
    test_invalid_json_returns_error()

    print("\nAll tests passed.")
