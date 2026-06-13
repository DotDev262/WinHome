import json
import os
import sys
from io import StringIO
from unittest.mock import mock_open, patch

src_path = os.path.join(os.path.dirname(__file__), "..", "src")
sys.path.append(src_path)
import plugin

sys.path.remove(src_path)


def test_check_installed_true():
    request = {"command": "check_installed", "requestId": "123", "args": {}}
    with (
        patch("sys.stdin", StringIO(json.dumps(request))),
        patch("sys.stdout", new_callable=StringIO) as mock_stdout,
        patch("os.environ.get", return_value="C:\\Users\\test\\AppData\\Roaming"),
        patch("os.path.exists", return_value=True),
    ):
        plugin.main()
        output = json.loads(mock_stdout.getvalue())
        assert output["installed"] is True
        assert output["requestId"] == "123"
        assert "success" not in output
        assert "data" not in output


def test_check_installed_false():
    request = {"command": "check_installed", "requestId": "124", "args": {}}
    with (
        patch("sys.stdin", StringIO(json.dumps(request))),
        patch("sys.stdout", new_callable=StringIO) as mock_stdout,
        patch("os.environ.get", return_value="C:\\Users\\test\\AppData\\Roaming"),
        patch("os.path.exists", return_value=False),
    ):
        plugin.main()
        output = json.loads(mock_stdout.getvalue())
        assert output["installed"] is False
        assert "success" not in output


def test_apply_dry_run_changed():
    request = {"command": "apply", "requestId": "125", "args": {"settings": {"Hotkey": "Alt+Space"}, "dryRun": True}}
    existing_settings = '{"Hotkey": "Ctrl+Space"}'
    with (
        patch("sys.stdin", StringIO(json.dumps(request))),
        patch("sys.stdout", new_callable=StringIO) as mock_stdout,
        patch("os.environ.get", return_value="C:\\Users\\test\\AppData\\Roaming"),
        patch("os.path.exists", return_value=True),
        patch("builtins.open", mock_open(read_data=existing_settings)),
    ):
        plugin.main()
        output = json.loads(mock_stdout.getvalue())
        assert output["changed"] is True
        assert "success" not in output


def test_apply_no_changes():
    request = {"command": "apply", "requestId": "126", "args": {"settings": {"Hotkey": "Alt+Space"}, "dryRun": False}}
    existing_settings = '{"Hotkey": "Alt+Space"}'
    with (
        patch("sys.stdin", StringIO(json.dumps(request))),
        patch("sys.stdout", new_callable=StringIO) as mock_stdout,
        patch("os.environ.get", return_value="C:\\Users\\test\\AppData\\Roaming"),
        patch("os.path.exists", return_value=True),
        patch("builtins.open", mock_open(read_data=existing_settings)),
    ):
        plugin.main()
        output = json.loads(mock_stdout.getvalue())
        assert output["changed"] is False
        assert "success" not in output


def test_apply_writes_changes():
    request = {
        "command": "apply",
        "requestId": "127",
        "args": {"settings": {"Hotkey": "Alt+Space", "Plugins": {"Search": True}}, "dryRun": False},
    }
    existing_settings = '{"Hotkey": "Ctrl+Space", "Plugins": {"Search": false}}'

    mock_read = mock_open(read_data=existing_settings)
    mock_write = mock_open()

    with (
        patch("sys.stdin", StringIO(json.dumps(request))),
        patch("sys.stdout", new_callable=StringIO) as mock_stdout,
        patch("os.environ.get", return_value="C:\\Users\\test\\AppData\\Roaming"),
        patch("os.path.exists", return_value=True),
        patch("os.makedirs"),
        patch("builtins.open", mock_read),
        patch("os.fdopen", mock_write),
        patch("tempfile.mkstemp", return_value=(1, "temp.json")),
        patch("os.replace"),
    ):
        plugin.main()
        output = json.loads(mock_stdout.getvalue())
        assert output["changed"] is True
        assert "success" not in output

        # Verify write was called
        written_data = "".join(
            call.args[0] for call in mock_write().write.call_args_list if isinstance(call.args[0], str)
        )
        written_json = json.loads(written_data)
        assert written_json["Hotkey"] == "Alt+Space"
        assert written_json["Plugins"]["Search"] is True
