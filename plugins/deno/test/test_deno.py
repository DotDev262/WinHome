import os
import sys
import unittest
from unittest.mock import patch

src_path = os.path.abspath(os.path.join(os.path.dirname(__file__), "../src"))
sys.path.append(src_path)
import plugin

sys.path.remove(src_path)


class TestDenoPlugin(unittest.TestCase):
    @patch("plugin.shutil.which")
    def test_check_installed_true(self, mock_which):
        mock_which.return_value = "/usr/bin/deno"
        result = plugin.check_installed()
        self.assertTrue(result)

    @patch("plugin.shutil.which")
    def test_check_installed_false(self, mock_which):
        mock_which.return_value = None
        result = plugin.check_installed()
        self.assertFalse(result)

    @patch("plugin.get_deno_config_path")
    def test_apply_empty_config(self, mock_path):
        mock_path.return_value = "/fake/deno.json"
        args = {"settings": {}}
        result = plugin.apply_config(args, "req-456")

        self.assertEqual(result, {"requestId": "req-456", "changed": False})

    @patch("plugin.get_deno_config_path")
    @patch("plugin.read_deno_config")
    @patch("plugin.write_deno_config")
    def test_apply_config_no_changes_needed(self, mock_write, mock_read, mock_path):
        mock_path.return_value = "/fake/deno.json"
        mock_read.return_value = {"lint": {"rules": {"tags": ["recommended"]}}, "fmt": {"useTabs": True}}

        args = {"settings": {"lint": {"rules": {"tags": ["recommended"]}}, "fmt": {"useTabs": True}}}

        result = plugin.apply_config(args, "req-789")

        self.assertEqual(result, {"requestId": "req-789", "changed": False})
        mock_write.assert_not_called()

    @patch("plugin.get_deno_config_path")
    @patch("plugin.read_deno_config")
    @patch("plugin.write_deno_config")
    def test_apply_config_changes_needed(self, mock_write, mock_read, mock_path):
        mock_path.return_value = "/fake/deno.json"
        mock_read.return_value = {"lint": {"rules": {"tags": ["recommended"]}}}

        args = {
            "settings": {"lint": {"rules": {"tags": ["recommended"]}}, "fmt": {"useTabs": True}, "typeCheckOnRun": True}
        }

        result = plugin.apply_config(args, "req-abc")

        self.assertEqual(result["requestId"], "req-abc")
        self.assertTrue(result["changed"])

        mock_write.assert_called_once()
        written_config = mock_write.call_args[0][1]
        self.assertEqual(written_config["fmt"], {"useTabs": True})
        self.assertEqual(written_config["typeCheckOnRun"], True)

    @patch("plugin.get_deno_config_path")
    @patch("plugin.read_deno_config")
    @patch("plugin.write_deno_config")
    @patch("plugin.log")
    def test_apply_config_dry_run(self, mock_log, mock_write, mock_read, mock_path):
        mock_path.return_value = "/fake/deno.json"
        mock_read.return_value = {}

        args = {"settings": {"unstable": ["kv"]}, "dryRun": True}

        result = plugin.apply_config(args, "req-dry")

        self.assertEqual(result["requestId"], "req-dry")
        self.assertTrue(result["changed"])

        mock_write.assert_not_called()
        self.assertTrue(mock_log.called)

    def test_apply_config_invalid_settings(self):
        args = {"settings": "not-a-dict"}
        result = plugin.apply_config(args, "req-inv")
        self.assertFalse(result["changed"])
        self.assertEqual(result["error"], "settings must be an object")


if __name__ == "__main__":
    unittest.main()
