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


def test_check_installed_response_format():
    res = run_plugin(
        {
            "requestId": "1",
            "command": "check_installed",
            "args": {},
        }
    )

    assert res["requestId"] == "1"
    assert "installed" in res
    assert isinstance(res["installed"], bool)


def test_apply_config_dry_run():
    with tempfile.TemporaryDirectory() as tmp:
        os.environ["USERPROFILE"] = tmp

        res = run_plugin(
            {
                "requestId": "2",
                "command": "apply",
                "args": {
                    "dryRun": True,
                    "settings": {
                        "vGPU": True,
                        "networking": False,
                        "memoryInMB": 4096,
                    },
                },
            }
        )

        assert res["requestId"] == "2"
        assert res["changed"] is True


def test_invalid_settings_returns_error():
    res = run_plugin(
        {
            "requestId": "3",
            "command": "apply",
            "args": {
                "settings": None,
            },
        }
    )

    assert res["requestId"] == "3"
    assert "error" in res


def test_unknown_command():
    res = run_plugin(
        {
            "requestId": "4",
            "command": "unknown",
            "args": {},
        }
    )

    assert res["requestId"] == "4"
    assert "error" in res


def test_actual_file_write():
    with tempfile.TemporaryDirectory() as tmp:
        os.environ["USERPROFILE"] = tmp

        res = run_plugin(
            {
                "requestId": "5",
                "command": "apply",
                "args": {
                    "settings": {
                        "vGPU": False,
                        "networking": False,
                        "memoryInMB": 2048,
                    }
                },
            }
        )

        config_path = os.path.join(
            tmp,
            "Documents",
            "sandbox.wsb",
        )

        assert res["changed"] is True
        assert os.path.exists(config_path)

        with open(config_path, "r", encoding="utf-8") as f:
            content = f.read()

        assert "Configuration" in content
        assert "VGpu" in content
        assert "MemoryInMB" in content


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
