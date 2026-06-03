import json
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

    monkeypatch.setattr(shutil, "which", lambda x: "/usr/bin/choco")
    req = make_request("check_installed")
    result = plugin.check_installed(req["args"], req["requestId"])
    assert result["success"] is True
    assert result["data"] is True


def test_apply_dry_run_no_write(tmp_path, monkeypatch):
    config_file = tmp_path / "chocolatey.config"
    # Create empty base XML
    root = ET.Element(f"{{{plugin.CHOCO_NS}}}chocolatey")
    config_el = ET.SubElement(root, f"{{{plugin.CHOCO_NS}}}config")
    ET.SubElement(
        config_el,
        f"{{{plugin.CHOCO_NS}}}add",
        {"key": "cacheLocation", "value": "old_cache"},
    )
    tree = ET.ElementTree(root)
    tree.write(config_file, encoding="utf-8")

    monkeypatch.setattr(plugin, "get_config_path", lambda: str(config_file))

    args = {"settings": {"config": {"cacheLocation": "new_cache"}}}
    req = make_request("apply", args, dry_run=True)
    result = plugin.apply_config(req["args"], req["context"], req["requestId"])

    assert result["success"] is True
    assert result["changed"] is True

    # Assert file not modified
    parsed = ET.parse(config_file).getroot()
    cache_el = parsed.find(f".//{{{plugin.CHOCO_NS}}}add[@key='cacheLocation']")
    assert cache_el.get("value") == "old_cache"


def test_apply_merges_config(tmp_path, monkeypatch):
    config_file = tmp_path / "chocolatey.config"
    # Create empty base XML
    root = ET.Element(f"{{{plugin.CHOCO_NS}}}chocolatey")
    config_el = ET.SubElement(root, f"{{{plugin.CHOCO_NS}}}config")
    ET.SubElement(
        config_el,
        f"{{{plugin.CHOCO_NS}}}add",
        {"key": "cacheLocation", "value": "old_cache"},
    )
    tree = ET.ElementTree(root)
    tree.write(config_file, encoding="utf-8")

    monkeypatch.setattr(plugin, "get_config_path", lambda: str(config_file))

    args = {
        "settings": {
            "config": {"cacheLocation": "new_cache", "newKey": "new_val"},
            "features": {"checksumFiles": True},
        }
    }
    req = make_request("apply", args, dry_run=False)
    result = plugin.apply_config(req["args"], req["context"], req["requestId"])

    assert result["success"] is True
    assert result["changed"] is True

    # Assert file modified
    parsed = ET.parse(config_file).getroot()
    add_els = parsed.findall(f".//{{{plugin.CHOCO_NS}}}add")
    cache_val = next(el.get("value") for el in add_els if el.get("key") == "cacheLocation")
    newkey_val = next(el.get("value") for el in add_els if el.get("key") == "newKey")
    assert cache_val == "new_cache"
    assert newkey_val == "new_val"

    feat_els = parsed.findall(f".//{{{plugin.CHOCO_NS}}}feature")
    feat_val = next(el.get("enabled") for el in feat_els if el.get("name") == "checksumFiles")
    assert feat_val == "true"


def test_apply_idempotent(tmp_path, monkeypatch):
    config_file = tmp_path / "chocolatey.config"
    # Create empty base XML
    root = ET.Element(f"{{{plugin.CHOCO_NS}}}chocolatey")
    config_el = ET.SubElement(root, f"{{{plugin.CHOCO_NS}}}config")
    ET.SubElement(
        config_el,
        f"{{{plugin.CHOCO_NS}}}add",
        {"key": "cacheLocation", "value": "old_cache"},
    )
    tree = ET.ElementTree(root)
    tree.write(config_file, encoding="utf-8")

    monkeypatch.setattr(plugin, "get_config_path", lambda: str(config_file))

    args = {"settings": {"config": {"cacheLocation": "old_cache"}}}
    req = make_request("apply", args, dry_run=False)
    result = plugin.apply_config(req["args"], req["context"], req["requestId"])

    assert result["success"] is True
    assert result["changed"] is False


def test_apply_creates_file(tmp_path, monkeypatch):
    config_file = tmp_path / "chocolatey.config"
    # Ensure it doesn't exist
    if config_file.exists():
        config_file.unlink()

    monkeypatch.setattr(plugin, "get_config_path", lambda: str(config_file))

    args = {"settings": {"config": {"cacheLocation": "D:\\choco-cache"}}}
    req = make_request("apply", args, dry_run=False)
    result = plugin.apply_config(req["args"], req["context"], req["requestId"])

    assert result["success"] is True
    assert result["changed"] is True
    assert config_file.exists()

    parsed = ET.parse(config_file).getroot()
    add_els = parsed.findall(f".//{{{plugin.CHOCO_NS}}}add")
    cache_val = next(el.get("value") for el in add_els if el.get("key") == "cacheLocation")
    assert cache_val == "D:\\choco-cache"


def test_unknown_command_via_main(monkeypatch, capsys):
    request = json.dumps(
        {
            "requestId": "test-999",
            "command": "unknown_cmd",
            "args": {},
            "context": {},
        }
    )
    monkeypatch.setattr("sys.stdin", type("StdinMock", (), {"read": lambda self: request})())
    plugin.main()
    out = capsys.readouterr().out
    response = json.loads(out)

    assert response["success"] is False
    assert "Unknown command" in response["error"]


def test_corrupted_xml_recovery(tmp_path, monkeypatch):
    config_file = tmp_path / "chocolatey.config"
    config_file.write_text("<<<<< INVALID XML >>>>", encoding="utf-8")

    monkeypatch.setattr(plugin, "get_config_path", lambda: str(config_file))

    args = {"settings": {"config": {"cacheLocation": "recovered_cache"}}}
    req = make_request("apply", args, dry_run=False)
    result = plugin.apply_config(req["args"], req["context"], req["requestId"])

    assert result["success"] is True
    assert result["changed"] is True

    # Assert new valid XML
    parsed = ET.parse(config_file).getroot()
    add_els = parsed.findall(f".//{{{plugin.CHOCO_NS}}}add")
    cache_val = next(el.get("value") for el in add_els if el.get("key") == "cacheLocation")
    assert cache_val == "recovered_cache"

    # Assert backup exists
    backups = list(tmp_path.glob("*.bak"))
    assert len(backups) == 1
    assert "corrupted" in backups[0].name


def test_feature_enabled_false(tmp_path, monkeypatch):
    config_file = tmp_path / "chocolatey.config"
    # Create empty base XML
    root = ET.Element(f"{{{plugin.CHOCO_NS}}}chocolatey")
    tree = ET.ElementTree(root)
    tree.write(config_file, encoding="utf-8")

    monkeypatch.setattr(plugin, "get_config_path", lambda: str(config_file))

    args = {"settings": {"features": {"checksumFiles": False}}}
    req = make_request("apply", args, dry_run=False)
    result = plugin.apply_config(req["args"], req["context"], req["requestId"])

    assert result["success"] is True
    assert result["changed"] is True

    parsed = ET.parse(config_file).getroot()
    feat_els = parsed.findall(f".//{{{plugin.CHOCO_NS}}}feature")
    feat_val = next(el.get("enabled") for el in feat_els if el.get("name") == "checksumFiles")
    assert feat_val == "false"


def test_empty_args_handled(tmp_path, monkeypatch):
    config_file = tmp_path / "chocolatey.config"
    # Create empty base XML
    root = ET.Element(f"{{{plugin.CHOCO_NS}}}chocolatey")
    tree = ET.ElementTree(root)
    tree.write(config_file, encoding="utf-8")

    monkeypatch.setattr(plugin, "get_config_path", lambda: str(config_file))

    # args missing 'settings'
    req = make_request("apply", {}, dry_run=False)
    result = plugin.apply_config(req["args"], req["context"], req["requestId"])

    assert result["success"] is True
    assert result["changed"] is False

    # args with empty 'settings'
    req = make_request("apply", {"settings": {}}, dry_run=False)
    result = plugin.apply_config(req["args"], req["context"], req["requestId"])

    assert result["success"] is True
    assert result["changed"] is False
