import sys
import tempfile
from pathlib import Path

plugin_path = str(Path(__file__).resolve().parents[1] / "src")

sys.path.append(plugin_path)

import plugin

sys.path.remove(plugin_path)


def test_save_and_load_settings():
    with tempfile.TemporaryDirectory() as temp_dir:
        plugin.POSTMAN_DIR = temp_dir
        plugin.PACKAGES_DIR = str(Path(temp_dir) / "packages")

        package_dir = Path(plugin.PACKAGES_DIR) / "app-1.0.0"

        package_dir.mkdir(parents=True)

        settings = {"theme": "dark", "fontSize": 14}

        plugin.save_settings(settings)

        loaded = plugin.load_settings()

        assert loaded == settings


def test_deep_merge():
    original = {"editor": {"fontSize": 12}}

    updates = {"editor": {"theme": "dark"}}

    result = plugin.deep_merge(original, updates)

    assert result == {"editor": {"fontSize": 12, "theme": "dark"}}


def test_handle_apply_dry_run():
    with tempfile.TemporaryDirectory() as temp_dir:
        plugin.POSTMAN_DIR = temp_dir
        plugin.PACKAGES_DIR = str(Path(temp_dir) / "packages")

        package_dir = Path(plugin.PACKAGES_DIR) / "app-1.0.0"

        package_dir.mkdir(parents=True)

        plugin.save_settings({"theme": "light"})

        result = plugin.handle_apply({"theme": "dark"}, True, "test-id")

        assert result["changed"] is True

        settings = plugin.load_settings()

        assert settings["theme"] == "light"
