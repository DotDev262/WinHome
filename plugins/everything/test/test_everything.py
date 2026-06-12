import json
import os
import subprocess
import sys

PLUGIN = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "src", "plugin.py"))


def run_plugin(payload: dict):
    process = subprocess.Popen(
        [sys.executable, PLUGIN], stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True
    )

    stdout, stderr = process.communicate(json.dumps(payload))

    if stderr:
        print(stderr)

    return stdout.strip()


def test_check_installed():
    payload = {"requestId": "1", "command": "check_installed", "args": {}}

    out = run_plugin(payload)

    data = json.loads(out)

    assert data["success"] is True
    assert isinstance(data["data"], bool)


def test_apply_dry_run():
    payload = {
        "requestId": "1",
        "command": "apply",
        "context": {"dryRun": True},
        "args": {"settings": {"test_section": {"test_key": "abc123"}}},
    }

    out = run_plugin(payload)

    data = json.loads(out)

    assert data["success"] is True
    assert data["changed"] is True
