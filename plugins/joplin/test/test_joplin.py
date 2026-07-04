import sys
import tempfile
from pathlib import Path

plugin_path = str(Path(__file__).resolve().parents[1] / "src")

sys.path.append(plugin_path)

import plugin

sys.path.remove(plugin_path)


def test_save_and_load_settings():
    with tempfile.TemporaryDirectory() as temp_dir:
        plugin.CONFIG_DIR = temp_dir
        plugin.SETTINGS_FILE = str(Path(temp_dir) / "settings.json")

        test_settings = {"theme": 1, "locale": "en-GB"}

        plugin.save_settings(test_settings)
        loaded_settings = plugin.load_settings()
        assert loaded_settings == test_settings


def test_handle_apply_changes():
    with tempfile.TemporaryDirectory() as temp_dir:
        plugin.CONFIG_DIR = temp_dir
        plugin.SETTINGS_FILE = str(Path(temp_dir) / "settings.json")

        result = plugin.handle_apply({"theme": 2})

        assert result["changed"] is True
        settings = plugin.load_settings()
        assert settings["theme"] == 2


def test_handle_apply_dry_run():
    with tempfile.TemporaryDirectory() as temp_dir:
        plugin.CONFIG_DIR = temp_dir
        plugin.SETTINGS_FILE = str(Path(temp_dir) / "settings.json")

        plugin.save_settings({"theme": 1})

        result = plugin.handle_apply({"theme": 2}, dry_run=True)
        assert result["changed"] is True
        settings = plugin.load_settings()
        assert settings["theme"] == 1
