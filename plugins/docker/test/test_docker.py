import json
import os
import sys
import tempfile
import unittest
from unittest.mock import patch

# Add src directory to path to import plugin
_src_path = os.path.abspath(os.path.join(os.path.dirname(__file__), "../src"))
sys.path.append(_src_path)
import plugin

sys.path.remove(_src_path)


class TestDockerPlugin(unittest.TestCase):
    def setUp(self):
        self.temp_dir = tempfile.TemporaryDirectory()
        self.config_path = os.path.join(self.temp_dir.name, "settings.json")

    def tearDown(self):
        self.temp_dir.cleanup()

    @patch("plugin.shutil.which")
    def test_check_installed(self, mock_which):
        mock_which.return_value = "C:\\Program Files\\Docker\\Docker\\resources\\bin\\docker.exe"

        response = plugin.check_installed({}, "req-1")

        self.assertTrue(response["success"])
        self.assertTrue(response["data"])
        mock_which.assert_called()

    def test_merge_settings(self):
        target = {
            "wslEngineEnabled": False,
            "proxies": {"httpProxy": "http://oldproxy:80"},
            "registryMirrors": ["https://old.mirror"],
        }

        source = {
            "wslEngineEnabled": True,
            "experimental": True,
            "proxies": {"httpsProxy": "http://newproxy:443"},
            "registryMirrors": ["https://new.mirror"],
        }

        changed = plugin.merge_settings(target, source)

        self.assertTrue(changed)
        self.assertTrue(target["wslEngineEnabled"])
        self.assertTrue(target["experimental"])

        # Nested dicts should merge
        self.assertEqual(target["proxies"]["httpProxy"], "http://oldproxy:80")
        self.assertEqual(target["proxies"]["httpsProxy"], "http://newproxy:443")

        # Arrays should overwrite
        self.assertEqual(target["registryMirrors"], ["https://new.mirror"])

    @patch("plugin.get_config_path")
    def test_apply_config_dry_run(self, mock_get_path):
        mock_get_path.return_value = self.config_path

        # Write initial config
        with open(self.config_path, "w", encoding="utf-8") as f:
            json.dump({"wslEngineEnabled": False}, f)

        args = {"settings": {"wslEngineEnabled": True}}

        # Dry run
        response = plugin.apply_config(args, {"dryRun": True}, "req-2")
        self.assertTrue(response["success"])
        self.assertTrue(response["changed"])

        # Verify file was NOT changed
        with open(self.config_path, "r", encoding="utf-8") as f:
            content = json.load(f)
        self.assertFalse(content["wslEngineEnabled"])

    @patch("plugin.get_config_path")
    def test_apply_config_real_run(self, mock_get_path):
        mock_get_path.return_value = self.config_path

        args = {"settings": {"kubernetes": {"enabled": True}}}

        # Real run on missing file (should create it)
        response = plugin.apply_config(args, {"dryRun": False}, "req-3")
        self.assertTrue(response["success"])
        self.assertTrue(response["changed"])

        # Verify file WAS created and changed
        with open(self.config_path, "r", encoding="utf-8") as f:
            content = json.load(f)
        self.assertTrue(content["kubernetes"]["enabled"])

    @patch("plugin.get_config_path")
    def test_apply_config_no_changes(self, mock_get_path):
        mock_get_path.return_value = self.config_path

        # Write initial config
        with open(self.config_path, "w", encoding="utf-8") as f:
            json.dump({"kubernetes": {"enabled": True}}, f)

        args = {"settings": {"kubernetes": {"enabled": True}}}

        # Real run but no actual differences
        response = plugin.apply_config(args, {"dryRun": False}, "req-4")
        self.assertTrue(response["success"])
        self.assertFalse(response["changed"])

    @patch("plugin.get_config_path")
    def test_read_corrupted_config(self, mock_get_path):
        mock_get_path.return_value = self.config_path

        # Write corrupted config
        with open(self.config_path, "w", encoding="utf-8") as f:
            f.write("{ invalid json")

        args = {"settings": {"wslEngineEnabled": True}}

        # Should back up corrupted and apply new
        response = plugin.apply_config(args, {"dryRun": False}, "req-5")
        self.assertTrue(response["success"])
        self.assertTrue(response["changed"])

        # Verify file WAS reset and written
        with open(self.config_path, "r", encoding="utf-8") as f:
            content = json.load(f)
        self.assertTrue(content["wslEngineEnabled"])

        # Verify backup was created
        dir_name = os.path.dirname(self.config_path)
        backups = [f for f in os.listdir(dir_name) if f.startswith("settings.json.corrupted.")]
        self.assertEqual(len(backups), 1)


if __name__ == "__main__":
    unittest.main()
