import os
import json
import tempfile
import sys
import subprocess
import importlib.util
from unittest import mock
import pytest

PLUGIN_SCRIPT = os.path.abspath(os.path.join(os.path.dirname(__file__), '../src/plugin.py'))

# Dynamically load the plugin module cleanly without modifying sys.path
spec = importlib.util.spec_from_file_location("plugin", PLUGIN_SCRIPT)
plugin = importlib.util.module_from_spec(spec)
spec.loader.exec_module(plugin)

def run_plugin(request_data, appdata_path):
    """Helper to run the plugin via subprocess for full JSON protocol testing."""
    env = os.environ.copy()
    env["APPDATA"] = appdata_path
    
    result = subprocess.run(
        [sys.executable, PLUGIN_SCRIPT],
        input=json.dumps(request_data),
        capture_output=True,
        text=True,
        env=env
    )
    
    try:
        return json.loads(result.stdout)
    except Exception as e:
        print(f"Failed to parse stdout: {result.stdout}")
        print(f"Stderr: {result.stderr}")
        raise e


# --- Pure function tests (direct module calls) ---

def test_merge_settings():
    target = {"theme": "Light", "pluginSearchPaths": []}
    source = {"theme": "Dark", "hotkey": "Alt+Space"}
    
    changed = plugin.merge_settings(target, source)
    assert changed is True
    assert target["theme"] == "Dark"
    assert target["hotkey"] == "Alt+Space"
    assert target["pluginSearchPaths"] == []
    
    changed2 = plugin.merge_settings(target, {"theme": "Dark"})
    assert changed2 is False

@mock.patch("os.getenv")
def test_get_settings_path(mock_getenv):
    mock_getenv.return_value = "C:\Users\Test\AppData\Roaming"
    expected = "C:\Users\Test\AppData\Roaming\FlowLauncher\Settings.json"
    assert plugin.get_settings_path() == expected

@mock.patch("os.getenv")
@mock.patch("os.path.isdir")
def test_check_installed_true(mock_isdir, mock_getenv):
    mock_getenv.return_value = "C:\Users\Test\AppData\Roaming"
    mock_isdir.return_value = True
    
    response = plugin.check_installed({}, "req-1")
    assert response["success"] is True
    assert response["data"] is True


# --- Protocol tests (subprocess integration) ---

def test_apply_config_dry_run():
    with tempfile.TemporaryDirectory() as tmpdir:
        request = {
            "requestId": "req-2",
            "command": "apply",
            "args": {"settings": {"theme": "Dark", "builtinPlugins": {"Calculator": {"enabled": True}}}},
            "context": {"dryRun": True}
        }
        
        response = run_plugin(request, tmpdir)
        
        assert response["success"] is True
        assert response["changed"] is True
        assert response["requestId"] == "req-2"
        
        settings_file = os.path.join(tmpdir, "FlowLauncher", "Settings.json")
        assert not os.path.exists(settings_file)

def test_apply_config_real_run():
    with tempfile.TemporaryDirectory() as tmpdir:
        request = {
            "requestId": "req-3",
            "command": "apply",
            "args": {"settings": {"theme": "Dark", "hotkey": "Alt+Space"}},
            "context": {"dryRun": False}
        }
        
        response = run_plugin(request, tmpdir)
        
        assert response["success"] is True
        assert response["changed"] is True
        assert response["requestId"] == "req-3"
        
        settings_file = os.path.join(tmpdir, "FlowLauncher", "Settings.json")
        assert os.path.exists(settings_file)
        with open(settings_file, "r", encoding="utf-8") as f:
            data = json.load(f)
            assert data["theme"] == "Dark"
            assert data["hotkey"] == "Alt+Space"

if __name__ == '__main__':
    sys.exit(pytest.main(["-v", __file__]))