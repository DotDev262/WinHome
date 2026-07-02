import json
import os
import sys
import xml.etree.ElementTree as ET

src_path = os.path.join(os.path.dirname(__file__), "..", "src")
sys.path.append(src_path)
import plugin

sys.path.remove(src_path)


def test_check_installed_returns_bool(monkeypatch):
    import shutil

    monkeypatch.setattr(shutil, "which", lambda x: "C:/fake/nuget.exe")
    result = plugin.check_installed()
    assert isinstance(result, bool)
    assert result is True


def test_check_installed_false(monkeypatch):
    import shutil

    monkeypatch.setattr(shutil, "which", lambda x: None)
    monkeypatch.setattr(os.path, "exists", lambda x: False)
    result = plugin.check_installed()
    assert result is False


def test_apply_dry_run_no_write(tmp_path, monkeypatch):
    config_file = tmp_path / "NuGet.Config"
    root = ET.Element("configuration")
    ET.SubElement(root, "packageSources")
    tree = ET.ElementTree(root)
    tree.write(config_file, encoding="utf-8")
    monkeypatch.setattr(plugin, "get_config_path", lambda: str(config_file))
    args = {
        "dryRun": True,
        "settings": {"packageSources": [{"name": "nuget", "source": "https://api.nuget.org/v3/index.json"}]},
    }
    result = plugin.apply_config(args, "test-001")
    assert result["changed"] is True
    assert "success" not in result


def test_apply_success(tmp_path, monkeypatch):
    config_file = tmp_path / "NuGet.Config"
    root = ET.Element("configuration")
    ET.SubElement(root, "packageSources")
    tree = ET.ElementTree(root)
    tree.write(config_file, encoding="utf-8")
    monkeypatch.setattr(plugin, "get_config_path", lambda: str(config_file))
    args = {
        "dryRun": False,
        "settings": {"packageSources": [{"name": "nuget", "source": "https://api.nuget.org/v3/index.json"}]},
    }
    result = plugin.apply_config(args, "test-001")
    assert result["changed"] is True
    assert "success" not in result


def test_apply_noop(tmp_path, monkeypatch):
    config_file = tmp_path / "NuGet.Config"
    root = ET.Element("configuration")
    sources = ET.SubElement(root, "packageSources")
    ET.SubElement(sources, "add", {"key": "nuget", "value": "https://api.nuget.org/v3/index.json"})
    tree = ET.ElementTree(root)
    tree.write(config_file, encoding="utf-8")
    monkeypatch.setattr(plugin, "get_config_path", lambda: str(config_file))
    args = {
        "dryRun": False,
        "settings": {"packageSources": [{"name": "nuget", "source": "https://api.nuget.org/v3/index.json"}]},
    }
    result = plugin.apply_config(args, "test-001")
    assert result["changed"] is False


def test_invalid_settings_handled(tmp_path, monkeypatch):
    config_file = tmp_path / "NuGet.Config"
    root = ET.Element("configuration")
    tree = ET.ElementTree(root)
    tree.write(config_file, encoding="utf-8")
    monkeypatch.setattr(plugin, "get_config_path", lambda: str(config_file))
    result = plugin.apply_config({"settings": "invalid"}, "test-001")
    assert result["changed"] is False
    assert "success" not in result


def test_config_missing(tmp_path, monkeypatch):
    monkeypatch.setattr(plugin, "get_config_path", lambda: str(tmp_path / "missing.Config"))
    args = {
        "dryRun": False,
        "settings": {"packageSources": [{"name": "nuget", "source": "https://api.nuget.org/v3/index.json"}]},
    }
    result = plugin.apply_config(args, "test-001")
    assert result["changed"] is True


def test_empty_stdin(monkeypatch, capsys):
    monkeypatch.setattr("sys.stdin", __import__("io").StringIO(""))
    plugin.main()
    out = capsys.readouterr().out
    data = json.loads(out)
    assert data["requestId"] == "unknown"
    assert "error" in data
    assert "success" not in data


def test_malformed_json(monkeypatch, capsys):
    monkeypatch.setattr("sys.stdin", __import__("io").StringIO("not json"))
    plugin.main()
    out = capsys.readouterr().out
    data = json.loads(out)
    assert data["requestId"] == "unknown"
    assert "error" in data


def test_unknown_command(monkeypatch, capsys):
    payload = json.dumps({"requestId": "test-002", "command": "foobar", "args": {}})
    monkeypatch.setattr("sys.stdin", __import__("io").StringIO(payload))
    plugin.main()
    out = capsys.readouterr().out
    data = json.loads(out)
    assert "error" in data
    assert "success" not in data


def test_corrupted_xml_backup(tmp_path, monkeypatch):
    config_file = tmp_path / "NuGet.Config"
    config_file.write_text("not valid xml")
    monkeypatch.setattr(plugin, "get_config_path", lambda: str(config_file))
    result = plugin.apply_config({"settings": {}}, "test-001")
    assert "error" not in result
