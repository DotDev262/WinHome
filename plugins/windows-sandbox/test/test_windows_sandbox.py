import json
import os
import subprocess
import sys
import tempfile

PLUGIN = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "src", "plugin.py"))


def run_plugin(payload: dict) -> dict:
    result = subprocess.run([sys.executable, PLUGIN], input=json.dumps(payload), capture_output=True, text=True)

    return json.loads(result.stdout.strip())


def test_check_installed_response_format():
    res = run_plugin({"requestId": "1", "command": "check_installed", "args": {}, "context": {}})

    assert res["requestId"] == "1"
    assert res["success"]
    assert res["changed"] is False
    assert "data" in res
    assert isinstance(res["data"], bool)


def test_apply_config_dry_run():
    with tempfile.TemporaryDirectory() as tmp:
        os.environ["USERPROFILE"] = tmp

        res = run_plugin(
            {
                "requestId": "2",
                "command": "apply",
                "args": {"settings": {"vGPU": True, "networking": False, "memoryInMB": 4096}},
                "context": {"dryRun": True},
            }
        )

        assert res["requestId"] == "2"
        assert res["success"] is True
        assert isinstance(res["changed"], bool)
        assert "data" in res
        assert res["data"] is None
