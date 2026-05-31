import json
import os
import sys
import tempfile
import time
import unittest
from unittest.mock import patch

_src_path = os.path.abspath(
    os.path.join(os.path.dirname(__file__), "..", "src")
)
sys.path.append(_src_path)
import plugin

sys.path.remove(_src_path)


class TestPostmanPlugin(unittest.TestCase):
    def test_deep_merge_nested(self):
        target = {
            "theme": "light",
            "editor": {"fontSize": 12, "tabSize": 2},
        }
        source = {
            "theme": "dark",
            "editor": {"fontSize": 14},
            "twoPaneView": True,
        }
        changed = plugin.deep_merge(target, source)
        self.assertTrue(changed)
        self.assertEqual(target["theme"], "dark")
        self.assertEqual(target["editor"]["fontSize"], 14)
        self.assertEqual(target["editor"]["tabSize"], 2)
        self.assertTrue(target["twoPaneView"])

    def test_deep_merge_no_change(self):
        target = {"theme": "dark", "fontSize": 14}
        source = {"theme": "dark", "fontSize": 14}
        changed = plugin.deep_merge(target, source)
        self.assertFalse(changed)

    def test_discover_config_path_prefers_newest_package(self):
        with tempfile.TemporaryDirectory() as temp_dir:
            older = os.path.join(temp_dir, "packages", "v1.0.0", "settings.json")
            newer = os.path.join(temp_dir, "packages", "v2.0.0", "settings.json")
            os.makedirs(os.path.dirname(older), exist_ok=True)
            os.makedirs(os.path.dirname(newer), exist_ok=True)
            with open(older, "w", encoding="utf-8") as handle:
                handle.write("{}")
            time.sleep(0.01)
            with open(newer, "w", encoding="utf-8") as handle:
                handle.write('{"theme":"dark"}')

            with patch("plugin.get_postman_root", return_value=temp_dir):
                path = plugin.discover_config_path()

            self.assertEqual(path, newer)

    def test_discover_config_path_falls_back_to_storage(self):
        with tempfile.TemporaryDirectory() as temp_dir:
            storage_path = os.path.join(temp_dir, "storage", "settings.json")
            os.makedirs(os.path.dirname(storage_path), exist_ok=True)
            with open(storage_path, "w", encoding="utf-8") as handle:
                handle.write('{"theme":"light"}')

            with patch("plugin.get_postman_root", return_value=temp_dir):
                path = plugin.discover_config_path()

            self.assertEqual(path, storage_path)

    def test_discover_config_path_default_when_missing(self):
        with tempfile.TemporaryDirectory() as temp_dir:
            with patch("plugin.get_postman_root", return_value=temp_dir):
                path = plugin.discover_config_path()

            expected = os.path.join(
                temp_dir,
                "packages",
                plugin.DEFAULT_PACKAGE_DIR,
                "settings.json",
            )
            self.assertEqual(path, expected)

    @patch.dict(os.environ, {"APPDATA": "C:\\Users\\test\\AppData\\Roaming"})
    @patch("plugin.os.path.isdir")
    def test_check_installed_present(self, mock_isdir):
        mock_isdir.return_value = True
        response = plugin.check_installed({}, "req-1")
        self.assertTrue(response["success"])
        self.assertTrue(response["data"])

    @patch.dict(os.environ, {"APPDATA": "C:\\Users\\test\\AppData\\Roaming"})
    @patch("plugin.os.path.isdir")
    def test_check_installed_absent(self, mock_isdir):
        mock_isdir.return_value = False
        response = plugin.check_installed({}, "req-2")
        self.assertTrue(response["success"])
        self.assertFalse(response["data"])

    @patch("plugin.discover_config_path")
    @patch("plugin.read_json")
    @patch("plugin.write_json")
    def test_apply_config_writes(self, mock_write, mock_read, mock_discover):
        mock_discover.return_value = "dummy.json"
        mock_read.return_value = {"theme": "light"}

        response = plugin.apply_config(
            {"settings": {"theme": "dark", "fontSize": 16}},
            {"dryRun": False},
            "req-3",
        )

        self.assertTrue(response["success"])
        self.assertTrue(response["changed"])
        mock_write.assert_called_once_with(
            "dummy.json",
            {"theme": "dark", "fontSize": 16},
        )

    @patch("plugin.discover_config_path")
    @patch("plugin.read_json")
    @patch("plugin.write_json")
    def test_apply_config_dry_run(self, mock_write, mock_read, mock_discover):
        mock_discover.return_value = "dummy.json"
        mock_read.return_value = {"theme": "light"}

        response = plugin.apply_config(
            {"settings": {"theme": "dark"}},
            {"dryRun": True},
            "req-4",
        )

        self.assertTrue(response["success"])
        self.assertTrue(response["changed"])
        mock_write.assert_not_called()

    @patch("plugin.discover_config_path")
    @patch("plugin.read_json")
    @patch("plugin.write_json")
    def test_apply_config_no_change(self, mock_write, mock_read, mock_discover):
        mock_discover.return_value = "dummy.json"
        mock_read.return_value = {"theme": "dark"}

        response = plugin.apply_config(
            {"settings": {"theme": "dark"}},
            {"dryRun": False},
            "req-5",
        )

        self.assertTrue(response["success"])
        self.assertFalse(response["changed"])
        mock_write.assert_not_called()

    def test_apply_creates_missing_directory(self):
        with tempfile.TemporaryDirectory() as temp_dir:
            config_path = os.path.join(
                temp_dir,
                "packages",
                plugin.DEFAULT_PACKAGE_DIR,
                "settings.json",
            )
            with patch("plugin.discover_config_path", return_value=config_path):
                response = plugin.apply_config(
                    {"settings": {"theme": "dark", "fontSize": 14}},
                    {"dryRun": False},
                    "req-6",
                )

            self.assertTrue(response["success"])
            self.assertTrue(response["changed"])
            self.assertTrue(os.path.isfile(config_path))
            with open(config_path, "r", encoding="utf-8") as handle:
                data = json.load(handle)
            self.assertEqual(data["theme"], "dark")
            self.assertEqual(data["fontSize"], 14)

    def test_idempotent_write(self):
        with tempfile.TemporaryDirectory() as temp_dir:
            config_path = os.path.join(temp_dir, "settings.json")
            payload = {"theme": "dark", "fontSize": 14}

            plugin.write_json(config_path, payload)
            first = open(config_path, "r", encoding="utf-8").read()
            plugin.write_json(config_path, payload.copy())
            second = open(config_path, "r", encoding="utf-8").read()

            self.assertEqual(first, second)
            self.assertTrue(first.endswith("\n"))

    def test_read_corrupted_config(self):
        with tempfile.TemporaryDirectory() as temp_dir:
            config_path = os.path.join(temp_dir, "settings.json")
            with open(config_path, "w", encoding="utf-8") as handle:
                handle.write("{ invalid json")

            with patch("plugin.discover_config_path", return_value=config_path):
                response = plugin.apply_config(
                    {"settings": {"theme": "dark"}},
                    {"dryRun": False},
                    "req-7",
                )

            self.assertTrue(response["success"])
            self.assertTrue(response["changed"])
            with open(config_path, "r", encoding="utf-8") as handle:
                data = json.load(handle)
            self.assertEqual(data["theme"], "dark")
            backups = [
                name
                for name in os.listdir(temp_dir)
                if name.startswith("settings.json.corrupted.")
            ]
            self.assertEqual(len(backups), 1)


if __name__ == "__main__":
    unittest.main()
