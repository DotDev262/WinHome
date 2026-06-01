import json
import os
import subprocess
import sys
import tempfile

PLUGIN = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "src", "plugin.py"))


def run_plugin(payload: dict) -> dict:
    result = subprocess.run([sys.executable, PLUGIN], input=json.dumps(payload), capture_output=True, text=True)

    return json.loads(result.stdout.strip())


def test_check_installed():
    res = run_plugin({"requestId": "1", "command": "check_installed", "args": {}, "context": {}})

    assert res["requestId"] == "1"
    assert res["success"]
    assert "data" in res
    assert isinstance(res["data"], bool)


def test_apply_config_dry_run():
    with tempfile.TemporaryDirectory() as tmp:
        config_path = os.path.join(tmp, ".ripgreprc")
        os.environ["RIPGREP_CONFIG_PATH"] = config_path

        res = run_plugin(
            {
                "requestId": "2",
                "command": "apply",
                "args": {"settings": {"smart-case": True, "hidden": True, "max-columns": 150}},
                "context": {"dryRun": True},
            }
        )

        assert res["requestId"] == "2"
        assert res["success"]
        assert res["changed"]
        assert "data" in res
        assert not os.path.exists(config_path)


def test_apply_config_write():
    with tempfile.TemporaryDirectory() as tmp:
        config_path = os.path.join(tmp, ".ripgreprc")
        os.environ["RIPGREP_CONFIG_PATH"] = config_path

        res = run_plugin(
            {
                "requestId": "3",
                "command": "apply",
                "args": {"settings": {"smart-case": True, "hidden": True, "max-columns": 150}},
                "context": {"dryRun": False},
            }
        )

        assert res["requestId"] == "3"
        assert res["success"]
        assert res["changed"]
        assert os.path.exists(config_path)

        with open(config_path, "r", encoding="utf-8") as f:
            content = f.read()

        assert "--smart-case" in content
        assert "--hidden" in content
        assert "--max-columns=150" in content


def test_preserves_existing_unknown_flags():
    with tempfile.TemporaryDirectory() as tmp:
        config_path = os.path.join(tmp, ".ripgreprc")
        os.environ["RIPGREP_CONFIG_PATH"] = config_path

        with open(config_path, "w", encoding="utf-8") as f:
            f.write("--unknown-flag\n")

        res = run_plugin(
            {
                "requestId": "4",
                "command": "apply",
                "args": {"settings": {"smart-case": True}},
                "context": {"dryRun": False},
            }
        )

        assert res["success"]

        with open(config_path, "r", encoding="utf-8") as f:
            content = f.read()

        assert "--unknown-flag" in content
        assert "--smart-case" in content


def test_empty_stdin_returns_json_error():
    result = subprocess.run([sys.executable, PLUGIN], input="", capture_output=True, text=True)

    res = json.loads(result.stdout.strip())

    assert res["requestId"] == "unknown"
    assert not res["success"]
    assert "data" in res
    assert "error" in res
