import importlib.util
import json
import subprocess
import sys
from pathlib import Path

import pytest

PLUGIN_PATH = Path(__file__).resolve().parents[1] / "src" / "plugin.py"


def load_plugin_module():
    spec = importlib.util.spec_from_file_location("autohotkey_plugin", PLUGIN_PATH)
    module = importlib.util.module_from_spec(spec)
    assert spec.loader is not None
    spec.loader.exec_module(module)
    return module


def run_plugin(payload: dict) -> dict:
    result = subprocess.run(
        [sys.executable, str(PLUGIN_PATH)],
        input=json.dumps(payload),
        capture_output=True,
        text=True,
        check=False,
    )

    assert result.stdout.strip(), result.stderr
    return json.loads(result.stdout.strip())


def test_check_installed_reports_true_when_executable_is_found(monkeypatch):
    plugin = load_plugin_module()

    monkeypatch.setattr(plugin.shutil, "which", lambda name: None)
    monkeypatch.setattr(plugin.os.path, "exists", lambda path: path.endswith("AutoHotkey64.exe"))
    monkeypatch.setenv("PROGRAMFILES", r"C:\Program Files")
    monkeypatch.setenv("PROGRAMFILES(X86)", r"C:\Program Files (x86)")
    monkeypatch.setenv("LOCALAPPDATA", r"C:\Users\Test\AppData\Local")

    response = plugin.check_installed({}, "req-1")

    assert response == {
        "requestId": "req-1",
        "success": True,
        "changed": False,
        "data": True,
    }


def test_check_installed_reports_false_when_not_found(monkeypatch):
    plugin = load_plugin_module()

    monkeypatch.setattr(plugin.shutil, "which", lambda name: None)
    monkeypatch.setattr(plugin.os.path, "exists", lambda path: False)
    monkeypatch.setenv("PROGRAMFILES", r"C:\Program Files")
    monkeypatch.setenv("PROGRAMFILES(X86)", r"C:\Program Files (x86)")
    monkeypatch.setenv("LOCALAPPDATA", r"C:\Users\Test\AppData\Local")

    response = plugin.check_installed({}, "req-2")

    assert response["requestId"] == "req-2"
    assert response["success"] is True
    assert response["data"] is False


def test_apply_writes_new_script_with_ahk_v2_syntax(tmp_path):
    plugin = load_plugin_module()

    script_path = tmp_path / "Documents" / "AutoHotkey" / "main.ahk"
    response = plugin.apply_config(
        {
            "dry_run": False,
            "script_path": str(script_path),
            "hotkeys": {
                "#z": 'Run "https://www.google.com"',
                "!Space": 'Send "{Volume_Mute}"',
                "^!t": 'Run "cmd.exe"',
            },
            "hotstrings": {
                "::btw::": "by the way",
                "::sig::": "Best regards,\nJohn Doe",
                "::email::": "john@example.com",
            },
            "settings": {
                "icon_tip": "WinHome managed",
                "persistent": True,
                "detect_hidden_windows": "On",
            },
        },
        {"dryRun": False},
        "req-3",
    )

    assert response == {
        "requestId": "req-3",
        "success": True,
        "changed": True,
    }
    content = script_path.read_text(encoding="utf-8")
    assert content.startswith("#Requires AutoHotkey v2.0")
    assert "Persistent" in content
    assert 'DetectHiddenWindows "On"' in content
    assert 'TrayTip "WinHome managed"' in content
    assert "#z::" in content
    assert 'Run "https://www.google.com"' in content
    assert "::btw::by the way" in content
    assert "::email::john@example.com" in content
    assert "::sig::" in content
    assert "Best regards," in content
    assert "John Doe" in content


def test_apply_merges_hotkeys_without_losing_custom_sections(tmp_path):
    plugin = load_plugin_module()

    script_path = tmp_path / "AutoHotkey" / "main.ahk"
    script_path.parent.mkdir(parents=True, exist_ok=True)
    script_path.write_text(
        "\n".join(
            [
                "#Requires AutoHotkey v2.0",
                "",
                "; custom start",
                'MsgBox "Keep me"',
                "; custom end",
            ]
        )
        + "\n",
        encoding="utf-8",
    )

    response = plugin.apply_config(
        {
            "dry_run": False,
            "script_path": str(script_path),
            "hotkeys": {
                "#z": 'Run "https://www.google.com"',
            },
            "hotstrings": {},
            "settings": {},
        },
        {"dryRun": False},
        "req-4",
    )

    assert response["requestId"] == "req-4"
    assert response["success"] is True
    assert response["changed"] is True

    content = script_path.read_text(encoding="utf-8")
    assert 'MsgBox "Keep me"' in content
    assert "; custom start" in content
    assert "#z::" in content
    assert 'Run "https://www.google.com"' in content


def test_apply_dry_run_does_not_write_file(tmp_path, monkeypatch):
    plugin = load_plugin_module()

    script_path = tmp_path / "AutoHotkey" / "main.ahk"
    monkeypatch.setattr(
        plugin,
        "write_text",
        lambda *args, **kwargs: pytest.fail("write_text should not be called"),
    )

    response = plugin.apply_config(
        {
            "dry_run": True,
            "script_path": str(script_path),
            "hotkeys": {"#z": 'Run "https://www.google.com"'},
            "hotstrings": {},
            "settings": {},
        },
        {"dryRun": True},
        "req-5",
    )

    assert response == {
        "requestId": "req-5",
        "success": True,
        "changed": True,
    }
    assert not script_path.exists()


def test_apply_creates_missing_directories(tmp_path):
    plugin = load_plugin_module()

    script_path = tmp_path / "nested" / "AutoHotkey" / "main.ahk"
    response = plugin.apply_config(
        {
            "dry_run": False,
            "script_path": str(script_path),
            "hotkeys": {"#z": 'Run "https://www.google.com"'},
            "hotstrings": {},
            "settings": {},
        },
        {"dryRun": False},
        "req-6",
    )

    assert response["success"] is True
    assert response["changed"] is True
    assert script_path.exists()


def test_apply_is_idempotent_for_unchanged_script(tmp_path):
    plugin = load_plugin_module()

    script_path = tmp_path / "AutoHotkey" / "main.ahk"
    args = {
        "dry_run": False,
        "script_path": str(script_path),
        "hotkeys": {"#z": 'Run "https://www.google.com"'},
        "hotstrings": {"::btw::": "by the way"},
        "settings": {"persistent": True},
    }

    first = plugin.apply_config(args, {"dryRun": False}, "req-7")
    second = plugin.apply_config(args, {"dryRun": False}, "req-7")

    assert first["changed"] is True
    assert second["changed"] is False


def test_all_responses_include_request_id_and_errors_return_error_field(
    monkeypatch,
):
    plugin = load_plugin_module()

    success_response = plugin.process_request(
        {
            "requestId": "req-8",
            "command": "check_installed",
            "args": {},
        }
    )
    assert success_response["requestId"] == "req-8"

    monkeypatch.setattr(
        plugin,
        "apply_config",
        lambda args, context, request_id: (_ for _ in ()).throw(RuntimeError("boom")),
    )
    error_response = plugin.process_request(
        {
            "requestId": "req-9",
            "command": "apply",
            "args": {},
        }
    )

    assert error_response["requestId"] == "req-9"
    assert error_response["success"] is False
    assert "error" in error_response
    assert error_response["error"] == "Internal Script Error: boom"


def test_subprocess_protocol_round_trip(tmp_path):
    script_path = tmp_path / "AutoHotkey" / "main.ahk"

    response = run_plugin(
        {
            "requestId": "req-10",
            "command": "apply",
            "args": {
                "script_path": str(script_path),
                "hotkeys": {"#z": 'Run "https://www.google.com"'},
                "hotstrings": {},
                "settings": {},
            },
            "context": {"dryRun": False},
        }
    )

    assert response["requestId"] == "req-10"
    assert response["success"] is True
    assert response["changed"] is True
    assert script_path.exists()
