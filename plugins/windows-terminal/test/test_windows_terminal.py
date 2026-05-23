import json
import sys
import os

sys.path.insert(0, os.path.join(os.path.dirname(__file__), "..", "src"))
import plugin


def make_request(command, args=None, dry_run=False):
    return {
        "requestId": "test-001",
        "command": command,
        "args": args or {},
        "context": {"dryRun": dry_run},
    }


def test_check_installed_returns_bool():
    req = make_request("check_installed")
    result = plugin.check_installed(req["args"], req["requestId"])
    assert result["success"] is True
    assert isinstance(result["data"]["installed"], bool)


def test_apply_dry_run_no_write(tmp_path):
    settings_file = tmp_path / "settings.json"
    settings_file.write_text(json.dumps({"theme": "light"}))

    original_fn = plugin.get_active_settings_path
    plugin.get_active_settings_path = lambda: str(settings_file)

    req = make_request("apply", {"theme": "dark"}, dry_run=True)
    result = plugin.apply_config(req["args"], req["context"], req["requestId"])

    plugin.get_active_settings_path = original_fn

    assert result["success"] is True
    assert result["changed"] is False
    content = json.loads(settings_file.read_text())
    assert content["theme"] == "light"


def test_apply_merges_settings(tmp_path):
    settings_file = tmp_path / "settings.json"
    settings_file.write_text(json.dumps({"theme": "light"}))

    original_fn = plugin.get_active_settings_path
    plugin.get_active_settings_path = lambda: str(settings_file)

    req = make_request("apply", {"theme": "dark", "fontSize": 14})
    result = plugin.apply_config(req["args"], req["context"], req["requestId"])

    plugin.get_active_settings_path = original_fn

    assert result["success"] is True
    assert result["changed"] is True
    content = json.loads(settings_file.read_text())
    assert content["theme"] == "dark"
    assert content["fontSize"] == 14


def test_unknown_command_via_main(monkeypatch, capsys):
    import json
    import plugin

    request = json.dumps({
        "requestId": "test-999",
        "command": "unknown_cmd",
        "args": {},
        "context": {}
    })
    monkeypatch.setattr("sys.stdin", __import__("io").StringIO(request))
    plugin.main()
    out = capsys.readouterr().out
    response = json.loads(out)
    assert response["success"] is False
    assert "Unknown command" in response["error"]
