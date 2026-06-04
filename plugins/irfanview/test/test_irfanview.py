import configparser
import os
import sys
import tempfile
import unittest
from contextlib import contextmanager
from unittest.mock import patch

_src_path = os.path.join(os.path.dirname(__file__), "..", "src")
sys.path.append(_src_path)
try:
    import plugin
finally:
    sys.path.remove(_src_path)


@contextmanager
def mock_env():
    with tempfile.TemporaryDirectory() as td:
        with patch.dict(os.environ, {"APPDATA": td}):
            yield td


class TestIrfanViewPlugin(unittest.TestCase):
    def test_check_installed_via_dir(self):
        with mock_env() as td:
            self.assertFalse(plugin.check_installed())
            os.makedirs(os.path.join(td, "IrfanView"))
            self.assertTrue(plugin.check_installed())

    @patch("shutil.which")
    def test_check_installed_via_path(self, mock_which):
        with mock_env():
            mock_which.return_value = "/usr/bin/i_view64.exe"
            self.assertTrue(plugin.check_installed())

    def test_apply_settings_new_file(self):
        with mock_env() as td:
            settings = {"Others": {"ShowAllFiles": True, "ThumbnailSize": 200}, "Language": {"Language": "ENGLISH"}}
            plugin.apply_settings(settings, dry_run=False)

            ini_path = os.path.join(td, "IrfanView", "i_view64.ini")
            self.assertTrue(os.path.exists(ini_path))

            parser = configparser.ConfigParser(strict=False, interpolation=None)
            parser.optionxform = str
            parser.read(ini_path)

            self.assertEqual(parser.get("Others", "ShowAllFiles"), "1")
            self.assertEqual(parser.get("Others", "ThumbnailSize"), "200")
            self.assertEqual(parser.get("Language", "Language"), "ENGLISH")

    def test_apply_settings_deep_merge(self):
        with mock_env() as td:
            iv_dir = os.path.join(td, "IrfanView")
            os.makedirs(iv_dir)
            ini_path = os.path.join(iv_dir, "i_view32.ini")

            # Setup initial file
            parser = configparser.ConfigParser(strict=False, interpolation=None)
            parser.optionxform = str
            parser.add_section("Others")
            parser.set("Others", "ExistingKey", "ExistingValue")
            parser.set("Others", "ShowAllFiles", "0")
            parser.add_section("Unrelated")
            parser.set("Unrelated", "Key", "Value")

            with open(ini_path, "w") as f:
                parser.write(f, space_around_delimiters=False)

            settings = {"Others": {"ShowAllFiles": True}}

            plugin.apply_settings(settings, dry_run=False)

            parser2 = configparser.ConfigParser(strict=False, interpolation=None)
            parser2.optionxform = str
            parser2.read(ini_path)

            # Check deep merge
            self.assertEqual(parser2.get("Others", "ExistingKey"), "ExistingValue")
            self.assertEqual(parser2.get("Others", "ShowAllFiles"), "1")
            self.assertEqual(parser2.get("Unrelated", "Key"), "Value")

    def test_apply_settings_dry_run(self):
        with mock_env() as td:
            settings = {"Others": {"ShowAllFiles": True}}
            plugin.apply_settings(settings, dry_run=True)

            ini_path = os.path.join(td, "IrfanView", "i_view64.ini")
            self.assertFalse(os.path.exists(ini_path))

    def test_handle_request(self):
        with mock_env():
            # Test check_installed
            res = plugin.handle_request({"command": "check_installed", "requestId": "1"})
            self.assertEqual(res["requestId"], "1")
            self.assertIn("data", res)

            # Test apply
            res = plugin.handle_request(
                {
                    "command": "apply",
                    "requestId": "2",
                    "args": {"settings": {"Others": {"Test": True}}},
                    "context": {"dryRun": True},
                }
            )
            self.assertEqual(res["requestId"], "2")
            self.assertTrue(res.get("success"))


if __name__ == "__main__":
    unittest.main()
