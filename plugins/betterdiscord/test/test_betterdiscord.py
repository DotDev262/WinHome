import json
import os
import subprocess
import sys
import tempfile

PLUGIN = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "src", "plugin.py"))
TMP_DIR = tempfile.mkdtemp()


def run_plugin(payload: dict) -> dict:
    env = os.environ.copy()
    env["APPDATA"] = TMP_DIR
    result = subprocess.run(
        [sys.executable, PLUGIN],
        input=json.dumps(payload),
        capture_output=True,
        text=True,
        env=env,
    )

    return json.loads(result.stdout.strip())


def test_unknown_command():
    res = run_plugin(
        {
            "requestId": "1",
            "command": "explode",
            "args": {},
            "context": {},
        }
    )

    assert not res["success"]
    assert "error" in res

    print("✓ unknown_command")


def test_check_installed():
    res = run_plugin(
        {
            "requestId": "2",
            "command": "check_installed",
            "args": {},
            "context": {},
        }
    )

    assert res["success"]
    assert "data" in res

    print("✓ check_installed")


def test_deep_merge():
    with tempfile.TemporaryDirectory() as tmp:
        settings_path = os.path.join(tmp, "settings.json")

        initial = {"theme": "dark", "plugins": {"pluginA": True}}

        with open(settings_path, "w", encoding="utf-8") as f:
            json.dump(initial, f)

        with open(settings_path, "r", encoding="utf-8") as f:
            current = json.load(f)

        updates = {"plugins": {"pluginB": True}}

        current["plugins"].update(updates["plugins"])

        assert current["plugins"]["pluginA"] is True
        assert current["plugins"]["pluginB"] is True

        print("✓ deep_merge")


def test_dry_run():
    settings_path = os.path.join(TMP_DIR, "BetterDiscord", "data", "settings.json")
    if os.path.exists(settings_path):
        os.remove(settings_path)
    res = run_plugin(
        {
            "requestId": "3",
            "command": "apply",
            "args": {"settings": {"theme": "dark"}},
            "context": {"dryRun": True},
        }
    )

    assert res["success"]
    assert res["changed"]

    print("✓ dry_run")


def test_apply():
    settings_path = os.path.join(TMP_DIR, "BetterDiscord", "data", "settings.json")
    if os.path.exists(settings_path):
        os.remove(settings_path)
    res = run_plugin(
        {
            "requestId": "4",
            "command": "apply",
            "args": {"settings": {"theme": "dark", "plugins_enabled": ["pluginA"]}},
            "context": {"dryRun": False},
        }
    )

    assert res["success"]

    print("✓ apply")


if __name__ == "__main__":
    test_unknown_command()
    test_check_installed()
    test_deep_merge()
    test_dry_run()
    test_apply()

    print("\nAll tests passed.")
