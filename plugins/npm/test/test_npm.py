import json
import os
import sys
from io import StringIO
from unittest.mock import patch

sys.path.insert(
    0, os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
)
from src.plugin import main, read_npmrc, write_npmrc


def run_plugin(input_dict):
    """Helper to run the plugin with JSON input and return JSON output."""
    input_str = json.dumps(input_dict)

    # Mock stdin and stdout
    old_stdin = sys.stdin
    old_stdout = sys.stdout
    sys.stdin = StringIO(input_str)
    sys.stdout = StringIO()

    try:
        main()
        output_str = sys.stdout.getvalue()
        return json.loads(output_str)
    finally:
        sys.stdin = old_stdin
        sys.stdout = old_stdout


def test_read_npmrc(tmp_path):
    npmrc_content = (
        "registry=https://registry.npmjs.org/\n"
        "@scope:registry=https://custom.com/\n"
        "; comment\n"
        "save-exact=true\n"
    )
    npmrc_file = tmp_path / ".npmrc"
    npmrc_file.write_text(npmrc_content)

    config = read_npmrc(str(npmrc_file))
    assert config == {
        "registry": "https://registry.npmjs.org/",
        "@scope:registry": "https://custom.com/",
        "save-exact": "true",
    }


def test_write_npmrc(tmp_path):
    npmrc_file = tmp_path / ".npmrc"
    config = {
        "registry": "https://registry.npmjs.org/",
        "@scope:registry": "https://custom.com/",
        "save-exact": "true",
    }
    write_npmrc(str(npmrc_file), config)

    content = npmrc_file.read_text()
    assert "registry=https://registry.npmjs.org/" in content
    assert "@scope:registry=https://custom.com/" in content
    assert "save-exact=true" in content


@patch("src.plugin.get_npmrc_path")
def test_apply_command(mock_get_path, tmp_path):
    npmrc_file = tmp_path / ".npmrc"
    npmrc_file.write_text("registry=https://registry.npmjs.org/\n")
    mock_get_path.return_value = str(npmrc_file)

    request = {
        "requestId": "test-req-1",
        "command": "apply",
        "args": {"@scope:registry": "https://custom.com/", "save-exact": True},
    }

    response = run_plugin(request)
    assert response["success"] is True
    assert response["changed"] is True

    # Verify file was updated
    content = npmrc_file.read_text()
    assert "registry=https://registry.npmjs.org/" in content
    assert "@scope:registry=https://custom.com/" in content
    assert "save-exact=true" in content


@patch("src.plugin.get_npmrc_path")
def test_apply_dry_run(mock_get_path, tmp_path):
    npmrc_file = tmp_path / ".npmrc"
    npmrc_file.write_text("registry=https://registry.npmjs.org/\n")
    mock_get_path.return_value = str(npmrc_file)

    request = {
        "requestId": "test-req-2",
        "command": "apply",
        "args": {"save-exact": True},
        "context": {"dryRun": True},
    }

    response = run_plugin(request)
    assert response["success"] is True
    assert response["changed"] is True

    # Verify file was NOT updated
    content = npmrc_file.read_text()
    assert "registry=https://registry.npmjs.org/" in content
    assert "save-exact" not in content


@patch("shutil.which")
def test_check_installed(mock_which):
    mock_which.return_value = "/usr/local/bin/npm"
    request = {"requestId": "test-req-3", "command": "check_installed"}
    response = run_plugin(request)
    assert response["success"] is True
    assert response["data"] is True

    # Test when npm is not installed
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
