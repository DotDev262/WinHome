import json
import os
import subprocess

import tomlkit

PLUGIN_SCRIPT = os.path.join(os.path.dirname(__file__), "..", "src", "plugin.py")


def run_plugin(request_dict):
    """Helper to run the plugin script with a given JSON request via uv."""
    input_str = json.dumps(request_dict)
    # Use standard python to run since uv might not be perfectly nested here,
    # but the script uses inline dependencies so we must use 'uv run'
    result = subprocess.run(["uv", "run", PLUGIN_SCRIPT], input=input_str, text=True, capture_output=True)
    return json.loads(result.stdout) if result.stdout else None, result.stderr


def test_check_installed():
    req = {"requestId": "123", "command": "check_installed"}
    resp, stderr = run_plugin(req)
    assert resp is not None
    assert resp["requestId"] == "123"
    assert isinstance(resp["installed"], bool)


def test_apply_new_file(monkeypatch, tmp_path):
    # Set APPDATA to a temporary directory
    monkeypatch.setenv("APPDATA", str(tmp_path))

    settings = {"disable": ["pip", "npm"], "set_title": True, "git_repos": {"~/Projects/dotfiles": "main"}}

    req = {"requestId": "456", "command": "apply", "args": {"settings": settings}}

    resp, stderr = run_plugin(req)
    assert resp is not None
    assert resp["requestId"] == "456"
    assert resp.get("changed") is True

    config_file = tmp_path / "topgrade" / "topgrade.toml"
    assert config_file.exists()

    with open(config_file, "r", encoding="utf-8") as f:
        doc = tomlkit.load(f)

    assert doc["disable"] == ["pip", "npm"]
    assert doc["set_title"] is True
    assert doc["git_repos"]["~/Projects/dotfiles"] == "main"


def test_apply_merge_existing(monkeypatch, tmp_path):
    monkeypatch.setenv("APPDATA", str(tmp_path))

    config_dir = tmp_path / "topgrade"
    config_dir.mkdir(parents=True, exist_ok=True)
    config_file = config_dir / "topgrade.toml"

    initial_content = """
disable = ["gem"]
display_time = true

[git_repos]
"~/Projects/old" = "master"
"""
    with open(config_file, "w", encoding="utf-8") as f:
        f.write(initial_content)

    settings = {"disable": ["pip", "npm"], "git_repos": {"~/Projects/dotfiles": "main"}}

    req = {"requestId": "789", "command": "apply", "args": {"settings": settings}}

    resp, stderr = run_plugin(req)
    assert resp is not None
    assert resp.get("changed") is True

    with open(config_file, "r", encoding="utf-8") as f:
        doc = tomlkit.load(f)

    assert doc["disable"] == ["pip", "npm"]
    assert doc["display_time"] is True
    assert doc["git_repos"]["~/Projects/old"] == "master"
    assert doc["git_repos"]["~/Projects/dotfiles"] == "main"
