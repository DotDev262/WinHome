"""
Tests for the Espanso WinHome plugin.

Run with:  pytest test/test_espanso.py -v
"""

import json
import sys
from copy import deepcopy
from io import StringIO
from pathlib import Path
from unittest.mock import patch

import pytest

sys.path.insert(0, str(Path(__file__).parent.parent / "src"))
import plugin  # noqa: E402

# ---------------------------------------------------------------------------
# Fixtures
# ---------------------------------------------------------------------------


@pytest.fixture()
def tmp_appdata(tmp_path, monkeypatch):
    """Point APPDATA to a temp dir and return it."""
    monkeypatch.setenv("APPDATA", str(tmp_path))
    return tmp_path


@pytest.fixture()
def installed_appdata(tmp_appdata):
    """APPDATA with the espanso directory already created."""
    (tmp_appdata / "espanso").mkdir()
    return tmp_appdata


@pytest.fixture()
def existing_config():
    return {
        "matches": [
            {"trigger": ":email", "replace": "old@example.com"},
            {"trigger": ":hello", "replace": "Hello there!"},
        ],
        "global_vars": [
            {"name": "today", "type": "date", "params": {"format": "%Y-%m-%d"}},
        ],
    }


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------


def run_main(msg: dict) -> dict:
    """Feed one JSON message through main() and return the parsed response."""
    stdin_data = json.dumps(msg)
    with (
        patch("sys.stdin", StringIO(stdin_data)),
        patch("sys.stdout", new_callable=StringIO) as mock_stdout,
    ):
        try:
            plugin.main()
        except SystemExit:
            pass
        return json.loads(mock_stdout.getvalue().strip())


# ---------------------------------------------------------------------------
# get_base_yml_path
# ---------------------------------------------------------------------------


def test_get_base_yml_path(tmp_appdata):
    result = plugin.get_base_yml_path()
    assert result == tmp_appdata / "espanso" / "match" / "base.yml"


def test_get_base_yml_path_no_appdata(monkeypatch):
    monkeypatch.delenv("APPDATA", raising=False)
    with pytest.raises(EnvironmentError, match="APPDATA"):
        plugin.get_base_yml_path()


# ---------------------------------------------------------------------------
# is_espanso_installed
# ---------------------------------------------------------------------------


def test_is_espanso_installed_true(installed_appdata):
    assert plugin.is_espanso_installed() is True


def test_is_espanso_installed_false(tmp_appdata):
    assert plugin.is_espanso_installed() is False


def test_is_espanso_installed_no_appdata(monkeypatch):
    monkeypatch.delenv("APPDATA", raising=False)
    assert plugin.is_espanso_installed() is False


# ---------------------------------------------------------------------------
# read_config / write_config
# ---------------------------------------------------------------------------


def test_read_config_missing(tmp_path):
    assert plugin.read_config(tmp_path / "nonexistent.yml") == {}


def test_write_config_creates_dirs(tmp_path):
    path = tmp_path / "a" / "b" / "base.yml"
    plugin.write_config(path, {"matches": [{"trigger": ":t", "replace": "test"}]})
    assert path.exists()


def test_write_config_trailing_newline(tmp_path):
    path = tmp_path / "base.yml"
    plugin.write_config(path, {"matches": []})
    assert path.read_text(encoding="utf-8").endswith("\n")


# ---------------------------------------------------------------------------
# deep_merge_lists
# ---------------------------------------------------------------------------


def test_merge_lists_replaces():
    existing = [{"trigger": ":email", "replace": "old@example.com"}]
    incoming = [{"trigger": ":email", "replace": "new@example.com"}]
    result = plugin.deep_merge_lists(existing, incoming)
    assert result == [{"trigger": ":email", "replace": "new@example.com"}]


def test_merge_lists_appends():
    existing = [{"trigger": ":email", "replace": "old@example.com"}]
    incoming = [{"trigger": ":sig", "replace": "Best regards"}]
    result = plugin.deep_merge_lists(existing, incoming)
    assert len(result) == 2


def test_merge_lists_preserves_untouched():
    existing = [
        {"trigger": ":email", "replace": "me@example.com"},
        {"trigger": ":hello", "replace": "Hello!"},
    ]
    result = plugin.deep_merge_lists(existing, [{"trigger": ":email", "replace": "new@example.com"}])
    assert len(result) == 2
    assert result[1]["trigger"] == ":hello"


def test_merge_lists_custom_key():
    existing = [{"name": "today", "type": "date"}]
    incoming = [{"name": "today", "type": "shell"}]
    result = plugin.deep_merge_lists(existing, incoming, key="name")
    assert result == [{"name": "today", "type": "shell"}]


# ---------------------------------------------------------------------------
# merge_config
# ---------------------------------------------------------------------------


def test_merge_config_detects_change(existing_config):
    incoming = {"matches": [{"trigger": ":email", "replace": "new@example.com"}]}
    _, changed = plugin.merge_config(existing_config, incoming)
    assert changed is True


def test_merge_config_no_change(existing_config):
    _, changed = plugin.merge_config(
        existing_config,
        {
            "matches": deepcopy(existing_config["matches"]),
            "global_vars": deepcopy(existing_config["global_vars"]),
        },
    )
    assert changed is False


def test_merge_config_does_not_mutate(existing_config):
    original = deepcopy(existing_config)
    plugin.merge_config(existing_config, {"matches": [{"trigger": ":new", "replace": "x"}]})
    assert existing_config == original


def test_merge_config_preserves_existing(existing_config):
    merged, _ = plugin.merge_config(
        existing_config,
        {"matches": [{"trigger": ":email", "replace": "new@example.com"}]},
    )
    triggers = [m["trigger"] for m in merged["matches"]]
    assert ":hello" in triggers
    assert ":email" in triggers


# ---------------------------------------------------------------------------
# handle_check_installed
# ---------------------------------------------------------------------------


def test_handle_check_installed_true(installed_appdata):
    result = plugin.handle_check_installed("req-1", {})
    assert result == {
        "requestId": "req-1",
        "success": True,
        "changed": False,
        "data": {"installed": True},
    }


def test_handle_check_installed_false(tmp_appdata):
    result = plugin.handle_check_installed("req-2", {})
    assert result["data"]["installed"] is False
    assert result["success"] is True
    assert result["changed"] is False
    assert result["requestId"] == "req-2"


# ---------------------------------------------------------------------------
# handle_apply
# ---------------------------------------------------------------------------


def test_handle_apply_writes_file(tmp_path, monkeypatch):
    base_yml = tmp_path / "espanso" / "match" / "base.yml"
    monkeypatch.setattr(plugin, "get_base_yml_path", lambda: base_yml)
    result = plugin.handle_apply(
        "req-3",
        {"matches": [{"trigger": ":email", "replace": "me@example.com"}]},
    )
    assert result == {"requestId": "req-3", "success": True, "changed": True}
    assert base_yml.exists()


def test_handle_apply_dry_run_no_write(tmp_path, monkeypatch):
    base_yml = tmp_path / "espanso" / "match" / "base.yml"
    monkeypatch.setattr(plugin, "get_base_yml_path", lambda: base_yml)
    result = plugin.handle_apply("req-4", {"matches": [{"trigger": ":t", "replace": "x"}]}, dry_run=True)
    assert result == {"requestId": "req-4", "success": True, "changed": True}
    assert not base_yml.exists()


def test_handle_apply_no_change_no_write(tmp_path, monkeypatch, existing_config):
    base_yml = tmp_path / "espanso" / "match" / "base.yml"
    base_yml.parent.mkdir(parents=True)
    plugin.write_config(base_yml, existing_config)
    mtime = base_yml.stat().st_mtime
    monkeypatch.setattr(plugin, "get_base_yml_path", lambda: base_yml)
    result = plugin.handle_apply("req-5", existing_config)
    assert result == {"requestId": "req-5", "success": True, "changed": False}
    assert base_yml.stat().st_mtime == mtime


def test_handle_apply_merges_not_overwrites(tmp_path, monkeypatch, existing_config):
    base_yml = tmp_path / "espanso" / "match" / "base.yml"
    base_yml.parent.mkdir(parents=True)
    plugin.write_config(base_yml, existing_config)
    monkeypatch.setattr(plugin, "get_base_yml_path", lambda: base_yml)
    plugin.handle_apply(
        "req-6",
        {"matches": [{"trigger": ":email", "replace": "new@example.com"}]},
    )
    data = plugin.read_config(base_yml)
    triggers = [m["trigger"] for m in data["matches"]]
    assert ":hello" in triggers


def test_handle_apply_creates_missing_dirs(tmp_path, monkeypatch):
    base_yml = tmp_path / "deep" / "nested" / "base.yml"
    monkeypatch.setattr(plugin, "get_base_yml_path", lambda: base_yml)
    result = plugin.handle_apply("req-7", {"matches": [{"trigger": ":t", "replace": "test"}]})
    assert result["success"] is True
    assert base_yml.exists()


# ---------------------------------------------------------------------------
# Single-shot JSON-over-stdio protocol
# ---------------------------------------------------------------------------


def test_protocol_single_shot_check_installed(installed_appdata):
    resp = run_main({"requestId": "r1", "command": "check_installed", "args": {}})
    assert resp["requestId"] == "r1"
    assert resp["success"] is True
    assert resp["data"]["installed"] is True


def test_protocol_single_shot_apply(tmp_path, monkeypatch):
    base_yml = tmp_path / "espanso" / "match" / "base.yml"
    monkeypatch.setattr(plugin, "get_base_yml_path", lambda: base_yml)
    resp = run_main(
        {
            "requestId": "r2",
            "command": "apply",
            "args": {"matches": [{"trigger": ":email", "replace": "a@b.com"}]},
            "context": {},
        }
    )
    assert resp["requestId"] == "r2"
    assert resp["success"] is True
    assert resp["changed"] is True


def test_protocol_dry_run_via_context(tmp_path, monkeypatch):
    """dryRun must be read from context, not top-level."""
    base_yml = tmp_path / "espanso" / "match" / "base.yml"
    monkeypatch.setattr(plugin, "get_base_yml_path", lambda: base_yml)
    resp = run_main(
        {
            "requestId": "r3",
            "command": "apply",
            "args": {"matches": [{"trigger": ":t", "replace": "x"}]},
            "context": {"dryRun": True},
        }
    )
    assert resp["success"] is True
    assert resp["changed"] is True
    assert not base_yml.exists(), "dry-run must NOT write the file"


def test_protocol_dry_run_top_level_ignored(tmp_path, monkeypatch):
    """dry_run at top-level must NOT be honoured (WinHome sends context.dryRun)."""
    base_yml = tmp_path / "espanso" / "match" / "base.yml"
    monkeypatch.setattr(plugin, "get_base_yml_path", lambda: base_yml)
    run_main(
        {
            "requestId": "r4",
            "command": "apply",
            "args": {"matches": [{"trigger": ":t", "replace": "x"}]},
            "dry_run": True,  # old incorrect field — must be ignored
            "context": {},
        }
    )
    assert base_yml.exists(), "top-level dry_run must be ignored; file should have been written"


def test_protocol_unknown_command():
    resp = run_main({"requestId": "r5", "command": "bad_cmd", "args": {}})
    assert "error" in resp
    assert resp["requestId"] == "r5"


def test_protocol_request_id_propagated(installed_appdata):
    resp = run_main({"requestId": "my-unique-id", "command": "check_installed", "args": {}})
    assert resp["requestId"] == "my-unique-id"


def test_protocol_invalid_json():
    with (
        patch("sys.stdin", StringIO("not json")),
        patch("sys.stdout", new_callable=StringIO) as mock_stdout,
    ):
        try:
            plugin.main()
        except SystemExit:
            pass
        resp = json.loads(mock_stdout.getvalue().strip())
    assert "error" in resp
