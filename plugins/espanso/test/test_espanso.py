"""
Tests for the Espanso WinHome plugin.

Run with:  pytest test/test_espanso.py -v
"""

import json
import os
import sys
import textwrap
from io import StringIO
from pathlib import Path
from unittest.mock import MagicMock, patch

import pytest
import yaml

# Make src importable
sys.path.insert(0, str(Path(__file__).parent.parent / "src"))
import plugin  # noqa: E402  (after sys.path manipulation)


# ---------------------------------------------------------------------------
# Fixtures
# ---------------------------------------------------------------------------

@pytest.fixture()
def tmp_base_yml(tmp_path):
    """Return a Path pointing to a temporary base.yml (not yet created)."""
    return tmp_path / "espanso" / "match" / "base.yml"


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
# get_base_yml_path
# ---------------------------------------------------------------------------

def test_get_base_yml_path_uses_appdata(monkeypatch):
    monkeypatch.setenv("APPDATA", r"C:\Users\Test\AppData\Roaming")
    result = plugin.get_base_yml_path()
    assert result == Path(r"C:\Users\Test\AppData\Roaming\espanso\match\base.yml")


def test_get_base_yml_path_raises_without_appdata(monkeypatch):
    monkeypatch.delenv("APPDATA", raising=False)
    with pytest.raises(EnvironmentError, match="APPDATA"):
        plugin.get_base_yml_path()


# ---------------------------------------------------------------------------
# is_espanso_installed
# ---------------------------------------------------------------------------

def test_is_espanso_installed_true(tmp_path, monkeypatch):
    espanso_dir = tmp_path / "espanso"
    espanso_dir.mkdir()
    monkeypatch.setenv("APPDATA", str(tmp_path))
    assert plugin.is_espanso_installed() is True


def test_is_espanso_installed_false(tmp_path, monkeypatch):
    monkeypatch.setenv("APPDATA", str(tmp_path))
    assert plugin.is_espanso_installed() is False


def test_is_espanso_installed_no_appdata(monkeypatch):
    monkeypatch.delenv("APPDATA", raising=False)
    assert plugin.is_espanso_installed() is False


# ---------------------------------------------------------------------------
# read_yaml / write_yaml
# ---------------------------------------------------------------------------

def test_read_yaml_missing_file(tmp_path):
    result = plugin.read_yaml(tmp_path / "nonexistent.yml")
    assert result == {}


def test_read_yaml_empty_file(tmp_path):
    f = tmp_path / "empty.yml"
    f.write_text("", encoding="utf-8")
    assert plugin.read_yaml(f) == {}


def test_read_yaml_valid(tmp_path):
    f = tmp_path / "config.yml"
    f.write_text("matches:\n  - trigger: ':hi'\n    replace: Hello\n", encoding="utf-8")
    data = plugin.read_yaml(f)
    assert data == {"matches": [{"trigger": ":hi", "replace": "Hello"}]}


def test_write_yaml_creates_dirs(tmp_path):
    path = tmp_path / "a" / "b" / "c.yml"
    plugin.write_yaml(path, {"key": "value"})
    assert path.exists()
    loaded = yaml.safe_load(path.read_text(encoding="utf-8"))
    assert loaded == {"key": "value"}


def test_write_yaml_roundtrip(tmp_path, existing_config):
    path = tmp_path / "base.yml"
    plugin.write_yaml(path, existing_config)
    loaded = plugin.read_yaml(path)
    assert loaded == existing_config


# ---------------------------------------------------------------------------
# deep_merge_lists
# ---------------------------------------------------------------------------

def test_deep_merge_lists_replaces_existing():
    existing = [{"trigger": ":email", "replace": "old@example.com"}]
    incoming = [{"trigger": ":email", "replace": "new@example.com"}]
    result = plugin.deep_merge_lists(existing, incoming)
    assert result == [{"trigger": ":email", "replace": "new@example.com"}]


def test_deep_merge_lists_appends_new():
    existing = [{"trigger": ":email", "replace": "old@example.com"}]
    incoming = [{"trigger": ":sig", "replace": "Best regards"}]
    result = plugin.deep_merge_lists(existing, incoming)
    assert len(result) == 2
    assert result[1] == {"trigger": ":sig", "replace": "Best regards"}


def test_deep_merge_lists_preserves_untouched():
    existing = [
        {"trigger": ":email", "replace": "me@example.com"},
        {"trigger": ":hello", "replace": "Hello!"},
    ]
    incoming = [{"trigger": ":email", "replace": "new@example.com"}]
    result = plugin.deep_merge_lists(existing, incoming)
    assert len(result) == 2
    assert result[1] == {"trigger": ":hello", "replace": "Hello!"}


def test_deep_merge_lists_empty_existing():
    existing = []
    incoming = [{"trigger": ":t", "replace": "test"}]
    result = plugin.deep_merge_lists(existing, incoming)
    assert result == [{"trigger": ":t", "replace": "test"}]


def test_deep_merge_lists_empty_incoming():
    existing = [{"trigger": ":t", "replace": "test"}]
    result = plugin.deep_merge_lists(existing, [])
    assert result == existing


def test_deep_merge_lists_custom_key():
    existing = [{"name": "today", "type": "date"}]
    incoming = [{"name": "today", "type": "shell"}]
    result = plugin.deep_merge_lists(existing, incoming, key="name")
    assert result == [{"name": "today", "type": "shell"}]


# ---------------------------------------------------------------------------
# merge_config
# ---------------------------------------------------------------------------

def test_merge_config_no_overlap(existing_config):
    incoming = {
        "matches": [{"trigger": ":new", "replace": "New!"}],
    }
    merged, changed = plugin.merge_config(existing_config, incoming)
    assert changed is True
    triggers = [m["trigger"] for m in merged["matches"]]
    assert ":new" in triggers
    assert ":email" in triggers


def test_merge_config_with_overlap(existing_config):
    incoming = {
        "matches": [{"trigger": ":email", "replace": "updated@example.com"}],
    }
    merged, changed = plugin.merge_config(existing_config, incoming)
    assert changed is True
    email_match = next(m for m in merged["matches"] if m["trigger"] == ":email")
    assert email_match["replace"] == "updated@example.com"
    # existing :hello must still be present
    assert any(m["trigger"] == ":hello" for m in merged["matches"])


def test_merge_config_unchanged(existing_config):
    # Merging identical data should report no change
    incoming = {
        "matches": list(existing_config["matches"]),
        "global_vars": list(existing_config["global_vars"]),
    }
    _, changed = plugin.merge_config(existing_config, incoming)
    assert changed is False


def test_merge_config_does_not_mutate_existing(existing_config):
    original = json.loads(json.dumps(existing_config))
    plugin.merge_config(existing_config, {"matches": [{"trigger": ":x", "replace": "X"}]})
    assert existing_config == original


def test_merge_config_global_vars(existing_config):
    incoming = {
        "global_vars": [
            {"name": "today", "type": "date", "params": {"format": "%d/%m/%Y"}},
            {"name": "user", "type": "shell", "params": {"cmd": "whoami"}},
        ]
    }
    merged, changed = plugin.merge_config(existing_config, incoming)
    assert changed is True
    today = next(v for v in merged["global_vars"] if v["name"] == "today")
    assert today["params"]["format"] == "%d/%m/%Y"
    assert any(v["name"] == "user" for v in merged["global_vars"])


# ---------------------------------------------------------------------------
# handle_check_installed
# ---------------------------------------------------------------------------

def test_handle_check_installed_true(tmp_path, monkeypatch):
    (tmp_path / "espanso").mkdir()
    monkeypatch.setenv("APPDATA", str(tmp_path))
    result = plugin.handle_check_installed({})
    assert result == {"installed": True}


def test_handle_check_installed_false(tmp_path, monkeypatch):
    monkeypatch.setenv("APPDATA", str(tmp_path))
    result = plugin.handle_check_installed({})
    assert result == {"installed": False}


# ---------------------------------------------------------------------------
# handle_apply
# ---------------------------------------------------------------------------

def test_handle_apply_writes_file(tmp_path, monkeypatch):
    base_yml = tmp_path / "espanso" / "match" / "base.yml"
    monkeypatch.setattr(plugin, "get_base_yml_path", lambda: base_yml)

    args = {"matches": [{"trigger": ":email", "replace": "me@example.com"}]}
    result = plugin.handle_apply(args)

    assert result == {"success": True, "changed": True}
    assert base_yml.exists()
    data = yaml.safe_load(base_yml.read_text(encoding="utf-8"))
    assert data["matches"][0]["replace"] == "me@example.com"


def test_handle_apply_dry_run_does_not_write(tmp_path, monkeypatch):
    base_yml = tmp_path / "espanso" / "match" / "base.yml"
    monkeypatch.setattr(plugin, "get_base_yml_path", lambda: base_yml)

    args = {"matches": [{"trigger": ":email", "replace": "me@example.com"}]}
    result = plugin.handle_apply(args, dry_run=True)

    assert result == {"success": True, "changed": True}
    assert not base_yml.exists()  # must NOT have been written


def test_handle_apply_no_change_does_not_write(tmp_path, monkeypatch, existing_config):
    base_yml = tmp_path / "espanso" / "match" / "base.yml"
    base_yml.parent.mkdir(parents=True)
    plugin.write_yaml(base_yml, existing_config)
    mtime_before = base_yml.stat().st_mtime

    monkeypatch.setattr(plugin, "get_base_yml_path", lambda: base_yml)

    args = {
        "matches": existing_config["matches"],
        "global_vars": existing_config["global_vars"],
    }
    result = plugin.handle_apply(args)

    assert result == {"success": True, "changed": False}
    assert base_yml.stat().st_mtime == mtime_before


def test_handle_apply_merges_not_overwrites(tmp_path, monkeypatch, existing_config):
    base_yml = tmp_path / "espanso" / "match" / "base.yml"
    base_yml.parent.mkdir(parents=True)
    plugin.write_yaml(base_yml, existing_config)
    monkeypatch.setattr(plugin, "get_base_yml_path", lambda: base_yml)

    args = {"matches": [{"trigger": ":email", "replace": "new@example.com"}]}
    plugin.handle_apply(args)

    data = plugin.read_yaml(base_yml)
    triggers = [m["trigger"] for m in data["matches"]]
    assert ":hello" in triggers, "Existing :hello match should be preserved"
    assert ":email" in triggers


def test_handle_apply_creates_missing_dirs(tmp_path, monkeypatch):
    base_yml = tmp_path / "deep" / "nested" / "base.yml"
    monkeypatch.setattr(plugin, "get_base_yml_path", lambda: base_yml)

    args = {"matches": [{"trigger": ":t", "replace": "test"}]}
    result = plugin.handle_apply(args)

    assert result["success"] is True
    assert base_yml.exists()


# ---------------------------------------------------------------------------
# JSON-over-stdio integration
# ---------------------------------------------------------------------------

def _run_single_command(msg: dict) -> dict:
    """Feed one JSON message to main() and return the parsed response."""
    stdin_data = json.dumps(msg) + "\n"
    with patch("sys.stdin", StringIO(stdin_data)), \
         patch("sys.stdout", new_callable=StringIO) as mock_stdout:
        try:
            plugin.main()
        except SystemExit:
            pass
        output = mock_stdout.getvalue().strip()
    return json.loads(output)


def test_protocol_check_installed(tmp_path, monkeypatch):
    (tmp_path / "espanso").mkdir()
    monkeypatch.setenv("APPDATA", str(tmp_path))

    response = _run_single_command({"command": "check_installed", "args": {}})
    assert response == {"data": {"installed": True}}


def test_protocol_apply(tmp_path, monkeypatch):
    base_yml = tmp_path / "espanso" / "match" / "base.yml"
    monkeypatch.setattr(plugin, "get_base_yml_path", lambda: base_yml)

    msg = {
        "command": "apply",
        "args": {"matches": [{"trigger": ":email", "replace": "a@b.com"}]},
    }
    response = _run_single_command(msg)
    assert response["success"] is True
    assert response["changed"] is True


def test_protocol_unknown_command():
    response = _run_single_command({"command": "unknown_cmd", "args": {}})
    assert "error" in response
    assert "unknown_cmd" in response["error"].lower() or "unknown" in response["error"].lower()


def test_protocol_invalid_json():
    with patch("sys.stdin", StringIO("not json\n")), \
         patch("sys.stdout", new_callable=StringIO) as mock_stdout, \
         patch("sys.stdin", StringIO("not json\n")):
        # Feed bad JSON then EOF so main() exits
        lines = iter(["not json\n", ""])

        def fake_readline():
            try:
                return next(lines)
            except StopIteration:
                return ""

        mock_stdin = MagicMock()
        mock_stdin.readline.side_effect = fake_readline
        with patch("sys.stdin", mock_stdin):
            try:
                plugin.main()
            except SystemExit:
                pass
            output = mock_stdout.getvalue().strip()

    if output:
        response = json.loads(output)
        assert "error" in response