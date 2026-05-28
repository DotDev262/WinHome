import json
import os
import sys
from io import StringIO
from unittest.mock import patch

_src_path = os.path.abspath(
    os.path.join(os.path.dirname(__file__), "..", "src")
)
sys.path.append(_src_path)
import plugin

sys.path.remove(_src_path)


def run_plugin(input_dict):
    """Helper to run the plugin with JSON input and return JSON output."""
    input_str = json.dumps(input_dict)

    # Mock stdin and stdout
    old_stdin = sys.stdin
    old_stdout = sys.stdout
    sys.stdin = StringIO(input_str)
    sys.stdout = StringIO()

    try:
        plugin.main()
        output_str = sys.stdout.getvalue()
        return json.loads(output_str)
    finally:
        sys.stdin = old_stdin
        sys.stdout = old_stdout


def test_read_curlrc(tmp_path):
    curlrc_content = "proxy=http://proxy.example.com\n-k\n# comment\nsilent\n"
    curlrc_file = tmp_path / ".curlrc"
    curlrc_file.write_text(curlrc_content)

    config = plugin.read_curlrc(str(curlrc_file))
    assert config == {
        "proxy": "http://proxy.example.com",
        "-k": None,
        "silent": None,
    }


def test_write_curlrc(tmp_path):
    curlrc_file = tmp_path / ".curlrc"
    config = {"proxy": "http://proxy.example.com", "-k": None, "silent": "true"}
    plugin.write_curlrc(str(curlrc_file), config)

    content = curlrc_file.read_text()
    assert "proxy=http://proxy.example.com" in content
    assert "-k" in content.split("\n")
    assert "silent" in content.split("\n")


@patch("plugin.get_config_path")
def test_apply_command(mock_get_path, tmp_path):
    curlrc_file = tmp_path / ".curlrc"
    curlrc_file.write_text("proxy=http://proxy.example.com\n")
    mock_get_path.return_value = str(curlrc_file)

    request = {
        "requestId": "test-req-1",
        "command": "apply",
        "args": {"settings": {"proxy": "http://new.example.com", "-k": None}},
    }

    response = run_plugin(request)
    assert response["success"] is True
    assert response["changed"] is True
    assert response.get("data") is None

    # Verify file was updated
    content = curlrc_file.read_text()
    assert "proxy=http://new.example.com" in content
    assert "-k" in content.split("\n")


@patch("plugin.get_config_path")
def test_apply_dry_run(mock_get_path, tmp_path):
    curlrc_file = tmp_path / ".curlrc"
    curlrc_file.write_text("proxy=http://proxy.example.com\n")
    mock_get_path.return_value = str(curlrc_file)

    request = {
        "requestId": "test-req-2",
        "command": "apply",
        "args": {"settings": {"silent": True}},
        "context": {"dryRun": True},
    }

    response = run_plugin(request)
    assert response["success"] is True
    assert response["changed"] is True
    assert response.get("data") is None

    # Verify file was NOT updated
    content = curlrc_file.read_text()
    assert "proxy=http://proxy.example.com" in content
    assert "silent" not in content


@patch("shutil.which")
def test_check_installed(mock_which):
    mock_which.return_value = "/usr/bin/curl"
    request = {"requestId": "test-req-3", "command": "check_installed"}
    response = run_plugin(request)
    assert response["success"] is True
    assert response["data"] is True

    # Test when curl is not installed
    mock_which.return_value = None
    response = run_plugin(request)
    assert response["success"] is True
    assert response["data"] is False


def test_unknown_command():
    request = {"requestId": "test-req-4", "command": "unknown_cmd"}
    response = run_plugin(request)
    assert response["success"] is False
    assert "error" in response
    assert response["error"] == "Unknown command: unknown_cmd"
    assert response.get("data") is None
