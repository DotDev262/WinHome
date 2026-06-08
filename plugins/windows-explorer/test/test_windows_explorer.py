import os
import sys
import unittest
from unittest.mock import MagicMock, patch

src_path = os.path.abspath(os.path.join(os.path.dirname(__file__), "../src"))
sys.path.append(src_path)
import plugin

sys.path.remove(src_path)


class TestWindowsExplorerPlugin(unittest.TestCase):
    def setUp(self):
        self.maxDiff = None

    @patch("plugin.winreg")
    def test_check_installed_true(self, mock_winreg):
        result = plugin.check_installed()
        self.assertTrue(result)

    @patch("plugin.winreg")
    def test_check_installed_false(self, mock_winreg):
        mock_winreg.OpenKey.side_effect = FileNotFoundError
        result = plugin.check_installed()
        self.assertFalse(result)

    @patch("plugin.winreg")
    def test_apply_empty_config(self, mock_winreg):
        args = {"settings": {}}
        result = plugin.apply_config(args)
        self.assertEqual(result, {"changed": False})
        mock_winreg.OpenKey.assert_not_called()

    @patch("plugin.read_registry_values")
    @patch("plugin.winreg")
    def test_apply_config_no_changes_needed(self, mock_winreg, mock_read):
        mock_read.return_value = {"HideFileExt": 1, "Hidden": 2}
        args = {"settings": {"HideFileExt": True, "Hidden": 2}}
        result = plugin.apply_config(args)
        self.assertEqual(result, {"changed": False})
        mock_winreg.OpenKey.assert_not_called()

    @patch("plugin.read_registry_values")
    @patch("plugin.winreg")
    def test_apply_config_changes_needed(self, mock_winreg, mock_read):
        mock_read.return_value = {"HideFileExt": 0, "Hidden": 1, "ShowSuperHidden": 1}
        args = {"settings": {"HideFileExt": True, "Hidden": 2, "ShowSuperHidden": False}}
        mock_key = MagicMock()
        mock_winreg.OpenKey.return_value.__enter__.return_value = mock_key

        result = plugin.apply_config(args)

        self.assertEqual(result, {"changed": True})
        self.assertEqual(mock_winreg.SetValueEx.call_count, 3)
        mock_winreg.SetValueEx.assert_any_call(mock_key, "HideFileExt", 0, mock_winreg.REG_DWORD, 1)
        mock_winreg.SetValueEx.assert_any_call(mock_key, "Hidden", 0, mock_winreg.REG_DWORD, 2)
        mock_winreg.SetValueEx.assert_any_call(mock_key, "ShowSuperHidden", 0, mock_winreg.REG_DWORD, 0)

    @patch("plugin.read_registry_values")
    @patch("plugin.winreg")
    @patch("plugin.log")
    def test_apply_config_dry_run(self, mock_log, mock_winreg, mock_read):
        mock_read.return_value = {"HideFileExt": 0}
        args = {"dryRun": True, "settings": {"HideFileExt": True}}

        result = plugin.apply_config(args)

        self.assertEqual(result, {"changed": True})
        mock_winreg.OpenKey.assert_not_called()
        mock_log.assert_any_call("Dry run: Would update registry key HideFileExt to 1")

    @patch("plugin.read_registry_values")
    @patch("plugin.winreg")
    def test_apply_config_invalid_hidden(self, mock_winreg, mock_read):
        mock_read.return_value = {}
        args = {"settings": {"Hidden": 3}}

        result = plugin.apply_config(args)

        self.assertEqual(result, {"error": "Invalid value for Hidden: 3. Must be 1 or 2."})

    @patch("plugin.read_registry_values")
    @patch("plugin.winreg")
    def test_apply_config_registry_error(self, mock_winreg, mock_read):
        mock_read.return_value = {"HideFileExt": 0}
        args = {"settings": {"HideFileExt": True}}
        mock_winreg.OpenKey.side_effect = PermissionError("Access is denied")

        result = plugin.apply_config(args)

        self.assertTrue("Failed to write to registry" in result["error"])


if __name__ == "__main__":
    unittest.main()
