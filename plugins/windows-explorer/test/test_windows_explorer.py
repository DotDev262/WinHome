import importlib.util
import json
import unittest
from io import StringIO
from pathlib import Path
from unittest.mock import MagicMock

PLUGIN_PATH = Path(__file__).resolve().parents[1] / "src" / "plugin.py"

spec = importlib.util.spec_from_file_location("windows_explorer_plugin", PLUGIN_PATH)
plugin = importlib.util.module_from_spec(spec)
assert spec and spec.loader
spec.loader.exec_module(plugin)


def mock_registry(values: dict):
    mock_winreg = MagicMock()
    entries = [
        (name, value, 4 if name != "InstallDir" else 1)
        for name, value in values.items()
    ]

    def enum_value(_key, index):
        if index < len(entries):
            return entries[index]
        raise OSError("No more values")

    mock_winreg.EnumValue.side_effect = enum_value
    mock_winreg.HKEY_CURRENT_USER = "HKCU"
    mock_winreg.KEY_READ = 1
    mock_winreg.KEY_SET_VALUE = 2
    plugin.winreg = mock_winreg
    return mock_winreg


class TestWindowsExplorerPlugin(unittest.TestCase):
    def test_check_installed_always_true(self):
        result = plugin.check_installed({}, "req-1")
        self.assertTrue(result["success"])
        self.assertTrue(result["data"])
        self.assertEqual(result["requestId"], "req-1")

    def test_apply_config_skips_when_no_changes_needed(self):
        mock_winreg = mock_registry(
            {
                "Hidden": 2,
                "HideFileExt": 1,
                "ShowStatusBar": 1,
            }
        )

        result = plugin.apply_config(
            {
                "settings": {
                    "Hidden": 2,
                    "HideFileExt": True,
                    "ShowStatusBar": True,
                }
            },
            {"dryRun": False},
            "req-2",
        )

        self.assertTrue(result["success"])
        self.assertFalse(result["changed"])
        mock_winreg.SetValueEx.assert_not_called()

    def test_apply_config_updates_when_changes_needed(self):
        mock_winreg = mock_registry({"Hidden": 1, "HideFileExt": 1})

        result = plugin.apply_config(
            {"settings": {"Hidden": 2, "HideFileExt": False}},
            {"dryRun": False},
            "req-3",
        )

        self.assertTrue(result["success"])
        self.assertTrue(result["changed"])
        self.assertEqual(mock_winreg.SetValueEx.call_count, 2)

        hidden_call = mock_winreg.SetValueEx.call_args_list[0][0]
        self.assertEqual(hidden_call[1], "Hidden")
        self.assertEqual(hidden_call[4], 2)

        hide_ext_call = mock_winreg.SetValueEx.call_args_list[1][0]
        self.assertEqual(hide_ext_call[1], "HideFileExt")
        self.assertEqual(hide_ext_call[4], 0)

    def test_apply_config_dry_run_does_not_modify_registry(self):
        mock_winreg = mock_registry({"Hidden": 1})

        result = plugin.apply_config(
            {"settings": {"Hidden": 2}},
            {"dryRun": True},
            "req-4",
        )

        self.assertTrue(result["success"])
        self.assertTrue(result["changed"])
        mock_winreg.SetValueEx.assert_not_called()

    def test_apply_config_rejects_invalid_hidden_value(self):
        mock_registry({"Hidden": 1})

        result = plugin.apply_config(
            {"settings": {"Hidden": 3}},
            {},
            "req-5",
        )

        self.assertFalse(result["success"])
        self.assertIn("Invalid Hidden", result["error"])

    def test_apply_config_rejects_invalid_boolean_type(self):
        mock_registry({})

        result = plugin.apply_config(
            {"settings": {"ShowStatusBar": "yes"}},
            {},
            "req-6",
        )

        self.assertFalse(result["success"])
        self.assertIn("Must be a boolean", result["error"])

    def test_apply_config_handles_missing_registry_key(self):
        mock_winreg = MagicMock()
        mock_winreg.OpenKey.side_effect = FileNotFoundError()
        mock_winreg.HKEY_CURRENT_USER = "HKCU"
        mock_winreg.KEY_READ = 1
        mock_winreg.KEY_SET_VALUE = 2
        plugin.winreg = mock_winreg

        result = plugin.apply_config(
            {"settings": {"ShowStatusBar": True}},
            {},
            "req-7",
        )

        self.assertTrue(result["success"])
        self.assertTrue(result["changed"])
        mock_winreg.SetValueEx.assert_called_once()

    def test_apply_config_handles_missing_winreg_module(self):
        plugin.winreg = None

        result = plugin.apply_config(
            {"settings": {"ShowStatusBar": True}},
            {},
            "req-8",
        )

        self.assertFalse(result["success"])
        self.assertIn("winreg module not available", result["error"])

    @unittest.mock.patch("sys.stdin", new_callable=StringIO)
    @unittest.mock.patch("sys.stdout", new_callable=StringIO)
    def test_invalid_json(self, mock_stdout, mock_stdin):
        mock_stdin.write("not-json")
        mock_stdin.seek(0)

        plugin.main()

        response = json.loads(mock_stdout.getvalue())
        self.assertFalse(response["success"])
        self.assertIn("Failed to parse request", response["error"])


if __name__ == "__main__":
    unittest.main()
