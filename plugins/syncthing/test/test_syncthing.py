import json
import os
import subprocess
import sys
import tempfile
import xml.etree.ElementTree as ET
from pathlib import Path

PLUGIN_PATH = Path(__file__).parent.parent / "src" / "plugin.py"


def run_plugin(input_data, env=None):
    result = subprocess.run(
        [sys.executable, str(PLUGIN_PATH)],
        input=input_data,
        text=True,
        capture_output=True,
        env=env or os.environ.copy(),
    )
    return result.stdout.strip()


def test_empty_input():
    stdout = run_plugin("")
    response = json.loads(stdout)
    assert response["requestId"] == "unknown"
    assert response["error"] == "No input received"


def test_invalid_json():
    stdout = run_plugin("this is not json!")
    response = json.loads(stdout)
    assert response["requestId"] == "unknown"
    assert response["error"] == "Invalid JSON"


def test_unknown_command():
    request = {"requestId": "123", "command": "dance"}
    stdout = run_plugin(json.dumps(request))
    response = json.loads(stdout)
    assert response["requestId"] == "123"
    assert "Unknown command: dance" in response["error"]


def test_check_installed_response_format():
    request = {"requestId": "456", "command": "check_installed"}
    stdout = run_plugin(json.dumps(request))
    response = json.loads(stdout)
    assert response["requestId"] == "456"
    assert isinstance(response["installed"], bool)


def test_apply_config_dry_run():
    request = {"requestId": "789", "command": "apply", "args": {"dryRun": True, "settings": {"gui": {"enabled": True}}}}
    stdout = run_plugin(json.dumps(request))
    response = json.loads(stdout)
    assert response["requestId"] == "789"
    assert response["changed"] is True


def test_apply_config():
    with tempfile.TemporaryDirectory() as temp_dir:
        env = os.environ.copy()
        env["LOCALAPPDATA"] = temp_dir
        env["HOME"] = temp_dir

        request = {
            "requestId": "101",
            "command": "apply",
            "args": {"dryRun": False, "settings": {"gui": {"enabled": True, "user": "admin"}}},
        }

        stdout = run_plugin(json.dumps(request), env=env)
        response = json.loads(stdout)
        assert response["changed"] is True

        if os.name == "nt":
            config_file = Path(temp_dir) / "Syncthing" / "config.xml"
        else:
            config_file = Path(temp_dir) / ".config" / "syncthing" / "config.xml"

        assert config_file.exists()
        tree = ET.parse(config_file)
        assert tree.getroot().find("gui").find("enabled").text == "true"


def test_idempotent_apply():
    with tempfile.TemporaryDirectory() as temp_dir:
        env = os.environ.copy()
        env["LOCALAPPDATA"] = temp_dir
        env["HOME"] = temp_dir

        request = {
            "requestId": "202",
            "command": "apply",
            "args": {"dryRun": False, "settings": {"options": {"listenAddress": "default"}}},
        }

        run_plugin(json.dumps(request), env=env)

        stdout = run_plugin(json.dumps(request), env=env)
        response = json.loads(stdout)
        assert response["changed"] is False
