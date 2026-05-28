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
    if result.returncode != 0:
        print(f"Error output: {result.stderr}")
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
    assert res["success"] is True
    print("OK: check_installed")


def test_apply_config_dry_run():
    with tempfile.TemporaryDirectory() as tmp:
        os.environ["USERPROFILE"] = tmp

        res = run_plugin(
            {
                "requestId": "2",
                "command": "apply",
                "args": {"format": "$all", "nodejs": {"disabled": True}},
                "context": {"dryRun": True},
            }
        )

        assert res["success"]
        assert res["changed"]

        config_path = os.path.join(tmp, ".config", "starship.toml")
        assert not os.path.exists(config_path)

        print("OK: apply_config_dry_run")


def test_apply_config():
    with tempfile.TemporaryDirectory() as tmp:
        os.environ["USERPROFILE"] = tmp

        res = run_plugin(
            {
                "requestId": "3",
                "command": "apply",
                "args": {
                    "format": "$all",
                    "scan_timeout": 30,
                    "character": {"success_symbol": "[➜](bold green)"},
                },
                "context": {"dryRun": False},
            }
        )

        assert res["success"]
        assert res["changed"]

        config_path = os.path.join(tmp, ".config", "starship.toml")
        assert os.path.exists(config_path)

        with open(config_path, "r", encoding="utf-8") as f:
            content = f.read()

        assert 'format = "$all"' in content
        assert "scan_timeout = 30" in content
        assert "[character]" in content
        assert 'success_symbol = "[➜](bold green)"' in content

        print("OK: apply_config")


def test_idempotent_apply():
    with tempfile.TemporaryDirectory() as tmp:
        os.environ["USERPROFILE"] = tmp

        payload = {
            "requestId": "4",
            "command": "apply",
            "args": {"add_newline": True},
            "context": {"dryRun": False},
        }

        # First run should create/modify and return changed: true
        res1 = run_plugin(payload)
        assert res1["success"]
        assert res1["changed"]

        # Second run should return changed: false
        res2 = run_plugin(payload)
        assert res2["success"]
        assert not res2["changed"]

        print("OK: idempotent_apply")


def test_unknown_command():
    res = run_plugin(
        {"requestId": "5", "command": "explode", "args": {}, "context": {}}
    )

    assert not res["success"]
    assert "error" in res
    print("OK: unknown_command")


if __name__ == "__main__":
    test_check_installed()
    test_apply_config_dry_run()
    test_apply_config()
    test_idempotent_apply()
    test_unknown_command()
    print("\nAll tests passed.")
