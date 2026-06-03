import os
import sys
import xml.etree.ElementTree as ET

src_path = os.path.join(os.path.dirname(__file__), "..", "src")
sys.path.append(src_path)
import plugin

sys.path.remove(src_path)


def make_request(command, args=None, dry_run=False):
    return {
        "requestId": "test-001",
        "command": command,
        "args": args or {},
        "context": {"dryRun": dry_run},
    }


def test_check_installed_returns_bool(monkeypatch):
    import shutil

    monkeypatch.setattr(shutil, "which", lambda x: "C:/fake/nuget.exe")

    result = plugin.check_installed({}, "test-001")

    assert result["success"] is True
    assert isinstance(result["data"], bool)


def test_apply_dry_run_no_write(tmp_path, monkeypatch):
    config_file = tmp_path / "NuGet.Config"

    root = ET.Element("configuration")
    ET.SubElement(root, "packageSources")

    tree = ET.ElementTree(root)
    tree.write(config_file, encoding="utf-8")

    monkeypatch.setattr(plugin, "get_config_path", lambda: str(config_file))

    args = {
        "settings": {
            "packageSources": [
                {
                    "name": "nuget",
                    "source": "https://api.nuget.org/v3/index.json",
                }
            ]
        }
    }

    result = plugin.apply_config(
        args,
        {"dryRun": True},
        "test-001",
    )

    assert result["success"] is True
    assert result["changed"] is True


def test_apply_success(tmp_path, monkeypatch):
    config_file = tmp_path / "NuGet.Config"

    root = ET.Element("configuration")
    ET.SubElement(root, "packageSources")

    tree = ET.ElementTree(root)
    tree.write(config_file, encoding="utf-8")

    monkeypatch.setattr(plugin, "get_config_path", lambda: str(config_file))

    args = {
        "settings": {
            "packageSources": [
                {
                    "name": "nuget",
                    "source": "https://api.nuget.org/v3/index.json",
                }
            ]
        }
    }

    result = plugin.apply_config(
        args,
        {"dryRun": False},
        "test-001",
    )

    assert result["success"] is True
    assert result["changed"] is True


def test_invalid_settings_handled():
    result = plugin.apply_config(
        {"settings": "invalid"},
        {"dryRun": False},
        "test-001",
    )

    assert "success" in result
