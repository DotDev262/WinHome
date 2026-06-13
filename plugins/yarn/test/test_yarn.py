import json
import os
import sys
from io import StringIO
from unittest.mock import patch

test_dir = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "src"))
sys.path.append(test_dir)
try:
    from src.plugin import main
finally:
    sys.path.remove(test_dir)


def run_plugin(input_dict):
    input_str = json.dumps(input_dict)

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


@patch("src.plugin._get_user_home")
def test_check_installed_via_config(mock_home, tmp_path):
    # Create berry config file
    berry = tmp_path / ".yarnrc.yml"
    berry.write_text("nodeLinker: node-modules\n", encoding="utf-8")
    mock_home.return_value = str(tmp_path)

    response = run_plugin({"requestId": "r1", "command": "check_installed"})
    assert response["success"] is True
    assert response["data"] is True
    assert response["changed"] is False


@patch("src.plugin._get_user_home")
def test_apply_dry_run_berry(mock_home, tmp_path):
    mock_home.return_value = str(tmp_path)
    # No config exists; should attempt to create .yarnrc.yml on apply

    request = {
        "requestId": "r2",
        "command": "apply",
        "args": {
            "settings": {
                "nodeLinker": "node-modules",
                "enableTelemetry": False,
                "compressionLevel": 0,
                "supportedArchitectures": {"os": "linux"},
            }
        },
        "context": {"dryRun": True},
    }

    response = run_plugin(request)
    assert response["success"] is True
    assert response["changed"] is True

    assert not (tmp_path / ".yarnrc.yml").exists()


@patch("src.plugin._get_user_home")
def test_apply_writes_berry_file_with_newline(mock_home, tmp_path):
    mock_home.return_value = str(tmp_path)

    request = {
        "requestId": "r3",
        "command": "apply",
        "args": {
            "settings": {
                "nodeLinker": "node-modules",
                "enableTelemetry": False,
                "compressionLevel": 7,
                "supportedArchitectures": {"cpu": "x64"},
            }
        },
        "context": {"dryRun": False},
    }

    response = run_plugin(request)
    assert response["success"] is True
    assert response["changed"] is True

    p = tmp_path / ".yarnrc.yml"
    assert p.exists()
    content = p.read_text(encoding="utf-8")
    assert content.endswith("\n")
    assert "nodeLinker:" in content
    assert "enableTelemetry:" in content


@patch("src.plugin._get_user_home")
def test_apply_classic_prefers_classic_if_present(mock_home, tmp_path):
    mock_home.return_value = str(tmp_path)

    # Create classic file
    (tmp_path / ".yarnrc").write_text("npmRegistryServer https://example.com\n", encoding="utf-8")

    request = {
        "requestId": "r4",
        "command": "apply",
        "args": {
            "settings": {
                "npmRegistryServer": "https://registry.yarnpkg.com",
            }
        },
        "context": {"dryRun": False},
    }

    response = run_plugin(request)
    assert response["success"] is True
    assert response["changed"] is True

    content = (tmp_path / ".yarnrc").read_text(encoding="utf-8")
    assert "npmRegistryServer https://registry.yarnpkg.com" in content
