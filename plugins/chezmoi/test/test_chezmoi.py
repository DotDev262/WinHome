import json
import os
import sys
import tempfile
from unittest.mock import patch

import pytest

src_path = os.path.join(os.path.dirname(__file__), "..", "src")
sys.path.append(src_path)
try:
    import plugin
finally:
    sys.path.remove(src_path)


@pytest.fixture
def mock_context():
    return {
        "os": "windows",
        "arch": "x64",
        "dryRun": False,
    }


@patch("plugin.shutil.which")
def test_check_installed_true(mock_which):
    mock_which.return_value = "/usr/local/bin/chezmoi"
    result = plugin.handle({"requestId": "req-1", "command": "check_installed", "args": {}})

    assert result["success"] is True
    assert result["data"] is True


@patch("plugin.shutil.which")
def test_check_installed_false(mock_which):
    mock_which.return_value = None
    result = plugin.handle({"requestId": "req-1", "command": "check_installed", "args": {}})

    assert result["success"] is True
    assert result["data"] is False


@patch("plugin.get_config_path")
def test_apply_config_creates_new(mock_get_config_path, mock_context):
    with tempfile.TemporaryDirectory() as temp_dir:
        config_path = os.path.join(temp_dir, "chezmoi.yaml")
        mock_get_config_path.return_value = config_path

        settings = {
            "sourceDir": "~/dotfiles",
            "merge": {
                "command": "nvim",
                "args": [
                    "-d",
                    "{{ .Destination }}",
                    "{{ .Source }}",
                    "{{ .Target }}",
                ],
            },
        }

        result = plugin.handle(
            {
                "requestId": "req-2",
                "command": "apply",
                "args": {"settings": settings},
                "context": mock_context,
            }
        )

        assert result["success"] is True
        assert result["changed"] is True
        assert os.path.exists(config_path)

        with open(config_path, "r") as f:
            content = f.read()
            assert "sourceDir: ~/dotfiles" in content
            assert "command: nvim" in content


@patch("plugin.get_config_path")
def test_apply_config_dry_run(mock_get_config_path):
    with tempfile.TemporaryDirectory() as temp_dir:
        config_path = os.path.join(temp_dir, "chezmoi.yaml")
        mock_get_config_path.return_value = config_path

        settings = {"sourceDir": "~/dotfiles"}

        result = plugin.handle(
            {
                "requestId": "req-3",
                "command": "apply",
                "args": {"settings": settings},
                "context": {"dryRun": True},
            }
        )

        assert result["success"] is True
        assert result["changed"] is True
        assert not os.path.exists(config_path)


@patch("plugin.get_config_path")
def test_apply_config_invalid_yaml_backup(mock_get_config_path, mock_context):
    with tempfile.TemporaryDirectory() as temp_dir:
        config_path = os.path.join(temp_dir, "chezmoi.yaml")
        mock_get_config_path.return_value = config_path

        # Write invalid YAML (using unclosed string / bad indentation that crashes parser)
        with open(config_path, "w") as f:
            f.write("invalid: \n  - yaml\n - format")

        settings = {"sourceDir": "~/dotfiles"}

        result = plugin.handle(
            {
                "requestId": "req-4",
                "command": "apply",
                "args": {"settings": settings},
                "context": mock_context,
            }
        )

        assert result["success"] is True
        assert result["changed"] is True

        # Check backup was created
        backups = [f for f in os.listdir(temp_dir) if f.endswith(".bak")]
        assert len(backups) == 1

        with open(os.path.join(temp_dir, backups[0]), "r") as f:
            assert f.read() == "invalid: \n  - yaml\n - format"


def test_empty_stdin():
    with patch("sys.stdin.read", return_value=""):
        with patch("sys.stdout.write") as mock_stdout:
            plugin.main()
            written = mock_stdout.call_args[0][0]
            result = json.loads(written)
            assert result["success"] is False
            assert "Empty input" in result["error"]


def test_apply_config_invalid_args():
    result = plugin.handle(
        {
            "requestId": "req-invalid-args",
            "command": "apply",
            "args": "not-a-dict",
            "context": {},
        }
    )
    assert result["success"] is False
    assert "args must be an object" in result["error"]


def test_apply_config_invalid_context():
    result = plugin.handle(
        {
            "requestId": "req-invalid-context",
            "command": "apply",
            "args": {},
            "context": "not-a-dict",
        }
    )
    assert result["success"] is False
    assert "context must be an object" in result["error"]


def test_apply_config_invalid_settings():
    result = plugin.handle(
        {
            "requestId": "req-invalid-settings",
            "command": "apply",
            "args": {"settings": "not-a-dict"},
            "context": {},
        }
    )
    assert result["success"] is False
    assert "settings must be an object" in result["error"]


def test_unknown_command():
    result = plugin.handle(
        {
            "requestId": "req-unknown",
            "command": "some_unknown_cmd",
            "args": {},
            "context": {},
        }
    )
    assert result["success"] is False
    assert "Unknown command: some_unknown_cmd" in result["error"]
