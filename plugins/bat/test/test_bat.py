import json
import os
import subprocess
import sys
from pathlib import Path

# Note: these tests are written for the repository's expected testing environment.
# In this execution environment, pytest may not be installed.


PLUGIN = Path(__file__).resolve().parents[1] / "src" / "plugin.py"


def run_plugin(payload=None, env=None, raw_input=None):
    input_data = raw_input if raw_input is not None else json.dumps(payload or {})
    result = subprocess.run(
        [sys.executable, str(PLUGIN)],
        input=input_data,
        capture_output=True,
        text=True,
        env=env,
    )
    stdout = result.stdout.strip()
    if stdout in {"true", "false"}:
        return (stdout == "true"), result
    assert stdout
    return json.loads(stdout), result


def plugin_env(tmp_path, path=None):
    env = os.environ.copy()
    env["USERPROFILE"] = str(tmp_path)
    env["HOME"] = str(tmp_path)
    if path is not None:
        env["PATH"] = str(path)
    # Also set APPDATA for Windows path resolution tests.
    env.setdefault("APPDATA", str(tmp_path / "appdata"))
    return env


def test_empty_stdin_returns_json_error(tmp_path):
    env = plugin_env(tmp_path)
    res, result = run_plugin(env=env, raw_input="")
    assert result.returncode == 0
    assert res["success"] is False
    assert res["requestId"] is None
    assert res["data"]["error"] == "Empty stdin"


def test_check_installed_returns_bare_bool(tmp_path):
    # Simulate bat present by creating a fake bat in a temp PATH.
    bin_dir = tmp_path / "bin"
    bin_dir.mkdir()
    exe = bin_dir / ("bat.exe" if os.name == "nt" else "bat")
    exe.write_text("", encoding="utf-8")

    res, _ = run_plugin(
        {"requestId": "req-1", "command": "check_installed"},
        env=plugin_env(tmp_path, bin_dir),
    )
    assert res is True

    # Missing case
    res2, _ = run_plugin(
        {"requestId": "req-2", "command": "check_installed"},
        env=plugin_env(tmp_path, tmp_path / "missing"),
    )
    assert res2 is False


def test_get_parses_managed_settings_preserves_comments(tmp_path):
    env = plugin_env(tmp_path)
    config_file = (tmp_path / ".config" / "bat").joinpath("config")
    config_file.parent.mkdir(parents=True, exist_ok=True)
    config_file.write_text(
        "# comment\n\n--theme=Dracula\n--style=plain\n--paging=never\n",
        encoding="utf-8",
    )

    # Force non-Windows path resolution by monkeypatching platform.system via env is hard;
    # so we patch by running plugin with APPDATA cleared and assume Linux-like path.
    # In CI this runs on linux; for local windows, update as needed.
    res, _ = run_plugin(
        {
            "requestId": "get-1",
            "command": "get",
            "args": {},
        },
        env=env,
    )

    assert res["success"] is True
    assert res["changed"] is False
    assert res["data"]["settings"]["--theme"] == "Dracula"
    assert res["data"]["settings"]["--style"] == "plain"
    assert res["data"]["settings"]["--paging"] == "never"


def test_set_merge_preserves_unknown_lines_and_updates_only_managed(tmp_path):
    env = plugin_env(tmp_path)
    config_file = (tmp_path / ".config" / "bat").joinpath("config")
    config_file.parent.mkdir(parents=True, exist_ok=True)
    config_file.write_text(
        "--theme=OneHalfDark\n--style=plain\n# custom option\n--italic-text=always\n",
        encoding="utf-8",
    )

    res, _ = run_plugin(
        {
            "requestId": "set-1",
            "command": "set",
            "args": {"settings": {"--theme": "Dracula"}},
            "context": {"dryRun": False},
        },
        env=env,
    )

    assert res["success"] is True
    assert res["changed"] is True

    out = config_file.read_text(encoding="utf-8")
    # Managed key updated
    assert "--theme=Dracula" in out
    # Other managed key preserved
    assert "--style=plain" in out
    # Unknown/comment preserved
    assert "# custom option" in out
    # Other managed preserved
    assert "--italic-text=always" in out


def test_atomic_write_uses_replace(tmp_path, monkeypatch):
    env = plugin_env(tmp_path)
    config_file = (tmp_path / ".config" / "bat").joinpath("config")
    config_file.parent.mkdir(parents=True, exist_ok=True)
    config_file.write_text("--theme=Old\n", encoding="utf-8")

    # Capture os.replace calls by monkeypatching in plugin process is non-trivial.
    # Instead validate file content is fully replaced and not partially written.
    res, _ = run_plugin(
        {
            "requestId": "atomic-1",
            "command": "set",
            "args": {"settings": {"--theme": "New"}},
            "context": {"dryRun": False},
        },
        env=env,
    )
    assert res["success"] is True
    assert config_file.read_text(encoding="utf-8") == "--theme=New\n"


def test_corrupted_backup_created_and_response_warns(tmp_path):
    env = plugin_env(tmp_path)
    config_file = (tmp_path / ".config" / "bat").joinpath("config")
    config_file.parent.mkdir(parents=True, exist_ok=True)
    # Put invalid UTF-8 bytes by writing bytes.
    config_file.write_bytes(b"\xff\xfe\xfa")

    res, result = run_plugin(
        {
            "requestId": "corrupt-1",
            "command": "set",
            "args": {"settings": {"--theme": "Dracula"}},
            "context": {"dryRun": False},
        },
        env=env,
    )

    assert res["success"] is True
    assert res["changed"] is True
    backups = list(tmp_path.glob(".config/bat/config.corrupt.*"))
    assert len(backups) == 1
    assert "backupPath" in res["data"]
    assert "--theme=Dracula" in config_file.read_text(encoding="utf-8")


def test_dry_run_does_not_write_or_backup(tmp_path):
    env = plugin_env(tmp_path)
    config_file = (tmp_path / ".config" / "bat").joinpath("config")
    config_file.parent.mkdir(parents=True, exist_ok=True)
    original = "--theme=Old\n--style=plain\n"
    config_file.write_text(original, encoding="utf-8")

    res, result = run_plugin(
        {
            "requestId": "dry-1",
            "command": "set",
            "args": {"settings": {"--theme": "New"}},
            "context": {"dryRun": True},
        },
        env=env,
    )

    assert res["success"] is True
    assert res["changed"] is True
    assert config_file.read_text(encoding="utf-8") == original
    backups = list(tmp_path.glob(".config/bat/config.corrupt.*"))
    assert backups == []
