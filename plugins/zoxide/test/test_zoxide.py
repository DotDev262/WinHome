import importlib.util
import json
from io import StringIO
from pathlib import Path
from unittest.mock import MagicMock, mock_open, patch

PLUGIN_PATH = Path(__file__).resolve().parents[1] / "src" / "plugin.py"

spec = importlib.util.spec_from_file_location("zoxide_plugin", PLUGIN_PATH)
plugin = importlib.util.module_from_spec(spec)
assert spec and spec.loader
spec.loader.exec_module(plugin)


def test_check_installed_returns_true_when_zoxide_is_found():
    with patch.object(
        plugin.shutil, "which", side_effect=[None, "C:/Tools/zoxide"]
    ):
        result = plugin.check_installed({}, "req-1")

    assert result["requestId"] == "req-1"
    assert result["success"] is True
    assert result["data"] == {"installed": True}


def test_check_installed_returns_false_when_zoxide_is_missing():
    with patch.object(plugin.shutil, "which", return_value=None):
        result = plugin.check_installed({}, "req-2")

    assert result["requestId"] == "req-2"
    assert result["success"] is True
    assert result["data"] == {"installed": False}


def test_apply_sets_env_vars_via_setx_when_values_differ():
    with (
        patch.dict(
            plugin.os.environ,
            {
                "USERPROFILE": "C:/Users/Test",
                "_ZO_MAX_DEPTH": "5",
                "_ZO_ECHO": "1",
                "_ZO_EXCLUDE_DIRS": "C:\\Windows;C:\\Program Files",
                "_ZO_RESOLVE_SYMLINKS": "1",
            },
            clear=True,
        ),
        patch.object(plugin, "update_profile_file", return_value=False),
        patch.object(
            plugin.subprocess,
            "run",
            return_value=MagicMock(returncode=0, stderr="", stdout=""),
        ) as mock_run,
    ):
        result = plugin.apply_config(
            {
                "env_vars": {
                    "_ZO_MAX_DEPTH": "10",
                    "_ZO_ECHO": "1",
                    "_ZO_EXCLUDE_DIRS": "C:\\Windows;C:\\Program Files",
                    "_ZO_RESOLVE_SYMLINKS": "1",
                },
                "init": {},
            },
            dry_run=False,
            request_id="req-3",
        )

    assert result["requestId"] == "req-3"
    assert result["success"] is True
    assert result["changed"] is True
    assert mock_run.call_count == 1
    assert mock_run.call_args.args[0] == ["setx", "_ZO_MAX_DEPTH", "10"]


def test_apply_skips_setx_when_env_vars_match():
    with (
        patch.dict(
            plugin.os.environ,
            {
                "USERPROFILE": "C:/Users/Test",
                "_ZO_MAX_DEPTH": "10",
                "_ZO_ECHO": "1",
                "_ZO_EXCLUDE_DIRS": "C:\\Windows;C:\\Program Files",
                "_ZO_RESOLVE_SYMLINKS": "1",
            },
            clear=True,
        ),
        patch.object(plugin, "update_profile_file", return_value=False),
        patch.object(
            plugin.subprocess,
            "run",
            side_effect=AssertionError(
                "setx should not be called when values already match"
            ),
        ),
    ):
        result = plugin.apply_config(
            {
                "env_vars": {
                    "_ZO_MAX_DEPTH": "10",
                    "_ZO_ECHO": "1",
                    "_ZO_EXCLUDE_DIRS": "C:\\Windows;C:\\Program Files",
                    "_ZO_RESOLVE_SYMLINKS": "1",
                },
                "init": {},
            },
            dry_run=False,
            request_id="req-4",
        )

    assert result["requestId"] == "req-4"
    assert result["success"] is True
    assert result["changed"] is False


def test_apply_skips_setx_on_non_windows():
    with (
        patch.dict(
            plugin.os.environ,
            {
                "USERPROFILE": "C:/Users/Test",
                "_ZO_MAX_DEPTH": "5",
            },
            clear=True,
        ),
        patch.object(plugin.sys, "platform", "linux"),
        patch.object(plugin, "update_profile_file", return_value=False),
        patch.object(
            plugin.subprocess,
            "run",
            side_effect=AssertionError(
                "setx should not run on non-Windows platforms"
            ),
        ),
    ):
        result = plugin.apply_config(
            {
                "env_vars": {"_ZO_MAX_DEPTH": "10"},
                "init": {},
            },
            dry_run=False,
            request_id="req-nonwin",
        )

    assert result["requestId"] == "req-nonwin"
    assert result["success"] is True


def test_apply_updates_powershell_init_line_when_flags_change():
    existing = 'Write-Host "hello"\nInvoke-Expression (& { (zoxide init powershell) })\n'
    opened = mock_open(read_data=existing)

    with (
        patch.dict(
            plugin.os.environ, {"USERPROFILE": "C:/Users/Test"}, clear=True
        ),
        patch("builtins.open", opened),
        patch.object(plugin.Path, "exists", return_value=True),
        patch.object(plugin.Path, "mkdir") as mock_mkdir,
    ):
        profile_path = Path(
            "C:/Users/Test/Documents/PowerShell/Microsoft.PowerShell_profile.ps1"
        )
        changed = plugin.update_profile_file(
            profile_path,
            plugin.build_init_line(
                "powershell",
                {"cmd": "z", "hook": "pwd", "no_cmd": False},
            ),
            dry_run=False,
        )

    assert changed is True
    mock_mkdir.assert_called_once()
    handle = opened()
    written = "".join(call.args[0] for call in handle.write.call_args_list)
    assert (
        "Invoke-Expression (& { (zoxide init powershell --cmd z) })" in written
    )
    assert 'Write-Host "hello"' in written


def test_apply_preserves_comment_lines_containing_zoxide_init():
    existing = "# How to use zoxide init\nInvoke-Expression (& { (zoxide init powershell) })\n"
    updated, changed = plugin.update_profile_content(
        existing,
        plugin.build_init_line(
            "powershell", {"cmd": "z", "hook": "pwd", "no_cmd": False}
        ),
    )

    assert changed is True
    assert "# How to use zoxide init" in updated
    assert (
        "Invoke-Expression (& { (zoxide init powershell --cmd z) })" in updated
    )


def test_apply_appends_init_line_if_not_present():
    updated, changed = plugin.update_profile_content(
        "Set-Location C:/Work\n",
        plugin.build_init_line(
            "bash", {"cmd": None, "hook": "pwd", "no_cmd": False}
        ),
    )

    assert changed is True
    assert "Set-Location C:/Work" in updated
    assert 'eval "$(zoxide init bash)"' in updated


def test_apply_dry_run_does_not_write_files_or_run_setx():
    opened = mock_open(read_data="Set-Location C:/Work\n")

    with (
        patch.dict(
            plugin.os.environ,
            {
                "USERPROFILE": "C:/Users/Test",
                "_ZO_MAX_DEPTH": "5",
            },
            clear=True,
        ),
        patch.object(plugin.subprocess, "run") as mock_run,
        patch("builtins.open", opened),
        patch.object(plugin.Path, "exists", return_value=True),
    ):
        result = plugin.apply_config(
            {
                "env_vars": {"_ZO_MAX_DEPTH": "10"},
                "init": {"cmd": "z", "hook": "pwd", "no_cmd": False},
            },
            dry_run=True,
            request_id="req-5",
        )

    assert result["requestId"] == "req-5"
    assert result["success"] is True
    assert result["changed"] is True
    mock_run.assert_not_called()
    handle = opened()
    handle.write.assert_not_called()


def test_apply_returns_changed_false_when_nothing_needs_updating():
    matching_line = plugin.build_init_line(
        "powershell", {"cmd": None, "hook": "pwd", "no_cmd": False}
    )
    existing = f"{matching_line}\n"
    opened = mock_open(read_data=existing)

    with (
        patch.dict(
            plugin.os.environ,
            {
                "USERPROFILE": "C:/Users/Test",
                "_ZO_MAX_DEPTH": "10",
                "_ZO_ECHO": "1",
                "_ZO_EXCLUDE_DIRS": "C:\\Windows;C:\\Program Files",
                "_ZO_RESOLVE_SYMLINKS": "1",
            },
            clear=True,
        ),
        patch.object(plugin.subprocess, "run") as mock_run,
        patch.object(
            plugin.shutil, "which", return_value="C:/Tools/zoxide.exe"
        ),
        patch("builtins.open", opened),
        patch.object(plugin.Path, "exists", return_value=True),
        patch.object(plugin, "update_profile_file", return_value=False),
    ):
        result = plugin.apply_config(
            {
                "env_vars": {
                    "_ZO_MAX_DEPTH": "10",
                    "_ZO_ECHO": "1",
                    "_ZO_EXCLUDE_DIRS": "C:\\Windows;C:\\Program Files",
                    "_ZO_RESOLVE_SYMLINKS": "1",
                },
                "init": {},
            },
            dry_run=False,
            request_id="req-6",
        )

    assert result["requestId"] == "req-6"
    assert result["success"] is True
    assert result["changed"] is False
    mock_run.assert_not_called()


def test_apply_builds_correct_init_line_with_flags():
    line = plugin.build_init_line(
        "powershell",
        {"cmd": "z", "hook": "pwd", "no_cmd": True},
    )

    assert (
        line
        == "Invoke-Expression (& { (zoxide init powershell --cmd z --no-cmd) })"
    )


def test_apply_handles_missing_profile_file_by_creating_it():
    profile_path = Path(
        "C:/Users/Test/Documents/PowerShell/Microsoft.PowerShell_profile.ps1"
    )
    opened = mock_open()

    with (
        patch.object(plugin.Path, "exists", return_value=False),
        patch("builtins.open", opened),
        patch.object(plugin.Path, "mkdir") as mock_mkdir,
    ):
        changed = plugin.update_profile_file(
            profile_path,
            plugin.build_init_line(
                "powershell", {"cmd": None, "hook": "pwd", "no_cmd": False}
            ),
            dry_run=False,
        )

    assert changed is True
    mock_mkdir.assert_called_once()
    handle = opened()
    handle.write.assert_called()


def test_process_request_returns_error_for_unknown_command():
    result = plugin.process_request(
        {"requestId": "req-7", "command": "explode", "args": {}}
    )

    assert result["requestId"] == "req-7"
    assert result["success"] is False
    assert "Unknown command" in result["error"]


def test_main_handles_pretty_printed_json_request():
    request = json.dumps(
        {
            "requestId": "req-main",
            "command": "check_installed",
            "args": {},
            "context": {},
        },
        indent=2,
    )

    with (
        patch("sys.stdin", StringIO(request)),
        patch("sys.stdout", new_callable=StringIO),
        patch.object(
            plugin.shutil, "which", side_effect=[None, "C:/Tools/zoxide"]
        ) as mock_which,
    ):
        plugin.main()
        output = (
            plugin.sys.stdout.getvalue()
            if hasattr(plugin.sys.stdout, "getvalue")
            else None
        )

    assert mock_which.call_count == 2

    if output is None:
        output = ""

    response = json.loads(output.strip())
    assert response["requestId"] == "req-main"
    assert response["success"] is True
    assert response["data"] == {"installed": True}
