import configparser
import importlib.util
import json
import os
import shutil

# Import the plugin script
import tempfile
import unittest
from io import StringIO
from pathlib import Path
from unittest.mock import patch

plugin_path = Path(__file__).parent.parent / "src" / "plugin.py"
spec = importlib.util.spec_from_file_location("plugin", plugin_path)
plugin = importlib.util.module_from_spec(spec)
spec.loader.exec_module(plugin)


class TestPipPlugin(unittest.TestCase):
    def setUp(self):
        self.temp_dir = tempfile.mkdtemp()
        self.mock_appdata = self.temp_dir
        self.pip_ini_path = os.path.join(self.temp_dir, "pip", "pip.ini")

    def tearDown(self):
        shutil.rmtree(self.temp_dir)

    @patch.object(plugin.shutil, "which")
    def test_check_installed_true(self, mock_which):
        mock_which.return_value = "/usr/bin/pip"
        response = plugin.check_installed({}, "req-1")
        self.assertTrue(response["success"])
        self.assertTrue(response["data"])
        self.assertEqual(response["requestId"], "req-1")

    @patch.object(plugin.shutil, "which")
    def test_check_installed_false(self, mock_which):
        mock_which.return_value = None
        response = plugin.check_installed({}, "req-2")
        self.assertTrue(response["success"])
        self.assertFalse(response["data"])

    @patch.object(plugin, "get_pip_ini_path")
    def test_apply_config_creates_new(self, mock_get_path):
        mock_get_path.return_value = self.pip_ini_path

        args = {
            "settings": {"index-url": "https://pypi.org/simple", "timeout": 60}
        }
        response = plugin.apply_config(args, {}, "req-3")

        self.assertTrue(response["success"])
        self.assertTrue(response["changed"])

        # Verify file contents
        config = configparser.ConfigParser()
        config.read(self.pip_ini_path)
        self.assertTrue(config.has_section("global"))
        self.assertEqual(
            config.get("global", "index-url"), "https://pypi.org/simple"
        )
        self.assertEqual(config.get("global", "timeout"), "60")

    @patch.object(plugin, "get_pip_ini_path")
    def test_apply_config_idempotency(self, mock_get_path):
        mock_get_path.return_value = self.pip_ini_path

        # First run
        args = {"settings": {"timeout": 120}}
        plugin.apply_config(args, {}, "req-4a")

        # Second run with exact same args
        response = plugin.apply_config(args, {}, "req-4b")

        self.assertTrue(response["success"])
        self.assertFalse(response["changed"])  # Should be false on second run

    @patch.object(plugin, "get_pip_ini_path")
    def test_apply_config_dry_run(self, mock_get_path):
        mock_get_path.return_value = self.pip_ini_path

        args = {"settings": {"timeout": 30}}
        context = {"dryRun": True}
        response = plugin.apply_config(args, context, "req-5")

        self.assertTrue(response["success"])
        self.assertTrue(response["changed"])
        self.assertFalse(
            os.path.exists(self.pip_ini_path)
        )  # Should not actually write

    @patch("sys.stdin", new_callable=StringIO)
    @patch("sys.stdout", new_callable=StringIO)
    def test_invalid_json(self, mock_stdout, mock_stdin):
        mock_stdin.write("INVALID { JSON")
        mock_stdin.seek(0)

        plugin.main()

        output = mock_stdout.getvalue()
        response = json.loads(output)

        self.assertFalse(response["success"])
        self.assertIn("Failed to parse request", response["error"])
        self.assertEqual(response["requestId"], "unknown")

    @patch.object(plugin, "get_pip_ini_path")
    def test_apply_config_corrupted_recovery(self, mock_get_path):
        mock_get_path.return_value = self.pip_ini_path

        # Create corrupted config
        os.makedirs(os.path.dirname(self.pip_ini_path), exist_ok=True)
        with open(self.pip_ini_path, "w", encoding="utf-8") as f:
            f.write("[global\ninvalid format")

        args = {"settings": {"timeout": "100"}}
        response = plugin.apply_config(args, {}, "req-corr")

        self.assertTrue(response["success"])
        self.assertTrue(response["changed"])

        # Verify it backed up the file
        backups = [
            f
            for f in os.listdir(os.path.dirname(self.pip_ini_path))
            if f.endswith(".bak")
        ]
        self.assertTrue(len(backups) > 0)

        # Verify new config
        config = configparser.ConfigParser()
        config.read(self.pip_ini_path)
        self.assertEqual(config.get("global", "timeout"), "100")


if __name__ == "__main__":
    unittest.main()
