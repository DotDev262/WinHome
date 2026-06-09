import json
import os
import shutil
import sys
import tempfile
import unittest
from unittest.mock import patch

plugin_src_path = os.path.join(os.path.dirname(os.path.dirname(__file__)), "src")
sys.path.append(plugin_src_path)
try:
    import plugin
finally:
    sys.path.remove(plugin_src_path)


class TestRainmeterPlugin(unittest.TestCase):
    def setUp(self):
        self.temp_dir = tempfile.mkdtemp()
        self.appdata = os.path.join(self.temp_dir, "AppData")
        os.makedirs(self.appdata)
        self.old_appdata = os.environ.get("APPDATA")
        os.environ["APPDATA"] = self.appdata
        self.config_dir = os.path.join(self.appdata, "Rainmeter")
        self.config_file = os.path.join(self.config_dir, "Rainmeter.ini")

    def tearDown(self):
        if self.old_appdata is not None:
            os.environ["APPDATA"] = self.old_appdata
        else:
            del os.environ["APPDATA"]
        shutil.rmtree(self.temp_dir)

    @patch("shutil.which")
    def test_check_installed_true_path(self, mock_which):
        mock_which.return_value = "C:\\Program Files\\Rainmeter\\Rainmeter.exe"
        res = plugin.check_installed()
        self.assertTrue(res)

    @patch("shutil.which")
    @patch("os.path.exists")
    def test_check_installed_true_default(self, mock_exists, mock_which):
        mock_which.return_value = None
        mock_exists.side_effect = lambda p: p.endswith("Rainmeter.exe")
        res = plugin.check_installed()
        self.assertTrue(res)

    @patch("shutil.which")
    @patch("os.path.exists")
    def test_check_installed_false(self, mock_exists, mock_which):
        mock_which.return_value = None
        mock_exists.return_value = False
        res = plugin.check_installed()
        self.assertFalse(res)

    def test_apply_config_creates_new_file(self):
        args = {
            "settings": {
                "Rainmeter": {"ConfigEditor": "notepad.exe"},
                "UserInterface": {"Language": "en"},
            }
        }
        res = plugin.apply_config(args, {}, "req-4")
        self.assertTrue(res["changed"])
        self.assertTrue(os.path.exists(self.config_file))

        with open(self.config_file, "r") as f:
            content = f.read()
            self.assertIn("[Rainmeter]", content)
            self.assertIn("ConfigEditor=notepad.exe", content)
            self.assertIn("[UserInterface]", content)
            self.assertIn("Language=en", content)

    def test_apply_config_merges_with_existing(self):
        os.makedirs(self.config_dir)
        with open(self.config_file, "w") as f:
            f.write("[Rainmeter]\nConfigEditor=old.exe\nSkinPath=C:\\Skins\n\n[ExistingSkin]\nActive=1\n")

        args = {
            "settings": {
                "Rainmeter": {"ConfigEditor": "notepad.exe", "DesktopWorkArea": "0,0,1920,1080"},
                "NewSkin": {"Draggable": 1},
            }
        }
        res = plugin.apply_config(args, {}, "req-5")
        self.assertTrue(res["changed"])

        with open(self.config_file, "r") as f:
            content = f.read()
            self.assertIn("ConfigEditor=notepad.exe", content)
            self.assertIn("SkinPath=C:\\Skins", content)
            self.assertIn("[ExistingSkin]", content)
            self.assertIn("Active=1", content)
            self.assertIn("[NewSkin]", content)
            self.assertIn("Draggable=1", content)

    def test_apply_config_no_changes(self):
        os.makedirs(self.config_dir)
        with open(self.config_file, "w") as f:
            f.write("[Rainmeter]\nConfigEditor=notepad.exe\n")

        args = {"settings": {"Rainmeter": {"ConfigEditor": "notepad.exe"}}}
        res = plugin.apply_config(args, {}, "req-6")
        self.assertFalse(res["changed"])

    def test_apply_config_dry_run(self):
        args = {"settings": {"Rainmeter": {"ConfigEditor": "notepad.exe"}}, "dryRun": True}
        res = plugin.apply_config(args, {}, "req-7")
        self.assertTrue(res["changed"])
        self.assertFalse(os.path.exists(self.config_file))

    def test_apply_config_corrupted_ini_creates_backup(self):
        os.makedirs(self.config_dir)
        with open(self.config_file, "w") as f:
            f.write("invalid ini content without section header\nkey=value\n")

        args = {"settings": {"Rainmeter": {"ConfigEditor": "notepad.exe"}}}
        res = plugin.apply_config(args, {}, "req-8")
        self.assertTrue(res["changed"])

        # Original should be overwritten with new section
        with open(self.config_file, "r") as f:
            content = f.read()
            self.assertIn("[Rainmeter]", content)

        # Backup should exist
        backups = [f for f in os.listdir(self.config_dir) if f.endswith(".bak")]
        self.assertEqual(len(backups), 1)
        with open(os.path.join(self.config_dir, backups[0]), "r") as f:
            self.assertEqual(f.read(), "invalid ini content without section header\nkey=value\n")

    def test_apply_config_invalid_settings_type(self):
        args = {"settings": ["not", "a", "dict"]}
        res = plugin.apply_config(args, {}, "req-9")
        self.assertIn("dictionary", res["error"])

    @patch("sys.stdout")
    @patch("sys.stdin")
    def test_main_missing_request_id(self, mock_stdin, mock_stdout):
        mock_stdin.read.return_value = '{"command": "check_installed"}'

        # mock stdout.write to capture the output
        output = []
        mock_stdout.write.side_effect = output.append

        plugin.main()

        result = json.loads(output[0])
        self.assertEqual(result["requestId"], "unknown")
        self.assertIn("installed", result)

    @patch("sys.stdout")
    @patch("sys.stdin")
    def test_main_invalid_json(self, mock_stdin, mock_stdout):
        mock_stdin.read.return_value = "invalid json"

        output = []
        mock_stdout.write.side_effect = output.append

        plugin.main()

        result = json.loads(output[0])
        self.assertIn("Invalid JSON", result["error"])

    @patch("sys.stdout")
    @patch("sys.stdin")
    def test_main_empty_input(self, mock_stdin, mock_stdout):
        mock_stdin.read.return_value = ""

        output = []
        mock_stdout.write.side_effect = output.append

        plugin.main()

        result = json.loads(output[0])
        self.assertIn("Empty input", result["error"])

    @patch("sys.stdout")
    @patch("sys.stdin")
    def test_main_unknown_command(self, mock_stdin, mock_stdout):
        mock_stdin.read.return_value = '{"command": "invalid_cmd", "requestId": "req-10"}'

        output = []
        mock_stdout.write.side_effect = output.append

        plugin.main()

        result = json.loads(output[0])
        self.assertIn("Unknown command", result["error"])


if __name__ == "__main__":
    unittest.main()
