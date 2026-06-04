import importlib.util
import unittest
from pathlib import Path
from unittest.mock import MagicMock, patch

PLUGIN_PATH = Path(__file__).resolve().parents[1] / "src" / "plugin.py"

spec = importlib.util.spec_from_file_location("7zip_plugin", PLUGIN_PATH)
plugin = importlib.util.module_from_spec(spec)
assert spec and spec.loader
spec.loader.exec_module(plugin)


class Test7ZipPlugin(unittest.TestCase):
    def test_check_installed_returns_true_when_7z_is_in_path(self):
        with patch.object(plugin.shutil, "which", return_value="C:/Program Files/7-Zip/7z.exe"):
            result = plugin.check_installed({}, "req-1")

        self.assertEqual(result["requestId"], "req-1")
        self.assertTrue(result["success"])
        self.assertFalse(result["changed"])
        self.assertTrue(result["data"])

    def test_check_installed_returns_true_when_registry_key_exists(self):
        mock_winreg = MagicMock()
        mock_winreg.OpenKey.return_value = MagicMock()
        plugin.winreg = mock_winreg

        with patch.object(plugin.shutil, "which", return_value=None):
            result = plugin.check_installed({}, "req-2")

        self.assertEqual(result["requestId"], "req-2")
        self.assertTrue(result["success"])
        self.assertFalse(result["changed"])
        self.assertTrue(result["data"])

    def test_check_installed_returns_false_when_missing(self):
        mock_winreg = MagicMock()
        mock_winreg.OpenKey.side_effect = FileNotFoundError()
        plugin.winreg = mock_winreg

        with patch.object(plugin.shutil, "which", return_value=None):
            result = plugin.check_installed({}, "req-3")

        self.assertEqual(result["requestId"], "req-3")
        self.assertTrue(result["success"])
        self.assertFalse(result["changed"])
        self.assertFalse(result["data"])

    def test_apply_config_skips_when_no_changes_needed(self):
        mock_winreg = MagicMock()
        registry_values = [
            ("CompressionLevel", 5, 4),
            ("CompressionMethod", "LZMA2", 1),
            ("EncryptHeaders", 1, 4),
        ]

        def enum_val(key, idx):
            if idx < len(registry_values):
                return registry_values[idx]
            raise OSError("No more values")

        mock_winreg.EnumValue.side_effect = enum_val
        plugin.winreg = mock_winreg

        result = plugin.apply_config(
            {"settings": {"CompressionLevel": 5, "CompressionMethod": "LZMA2", "EncryptHeaders": True}},
            {"dryRun": False},
            "req-4",
        )

        self.assertEqual(result["requestId"], "req-4")
        self.assertTrue(result["success"])
        self.assertFalse(result["changed"])
        mock_winreg.SetValueEx.assert_not_called()

    def test_apply_config_updates_when_changes_needed(self):
        mock_winreg = MagicMock()
        registry_values = [
            ("CompressionLevel", 5, 4),
        ]

        def enum_val(key, idx):
            if idx < len(registry_values):
                return registry_values[idx]
            raise OSError("No more values")

        mock_winreg.EnumValue.side_effect = enum_val
        plugin.winreg = mock_winreg

        result = plugin.apply_config(
            {
                "settings": {
                    "CompressionLevel": 9,
                }
            },
            {"dryRun": False},
            "req-5",
        )

        self.assertEqual(result["requestId"], "req-5")
        self.assertTrue(result["success"])
        self.assertTrue(result["changed"])

        mock_winreg.SetValueEx.assert_called_once()
        args = mock_winreg.SetValueEx.call_args[0]
        self.assertEqual(args[1], "CompressionLevel")
        self.assertEqual(args[3], 4)  # REG_DWORD
        self.assertEqual(args[4], 9)

    def test_apply_config_dry_run_does_not_modify_registry(self):
        mock_winreg = MagicMock()
        registry_values = [
            ("CompressionLevel", 5, 4),
        ]

        def enum_val(key, idx):
            if idx < len(registry_values):
                return registry_values[idx]
            raise OSError("No more values")

        mock_winreg.EnumValue.side_effect = enum_val
        plugin.winreg = mock_winreg

        result = plugin.apply_config(
            {
                "settings": {
                    "CompressionLevel": 9,
                }
            },
            {"dryRun": True},
            "req-6",
        )

        self.assertEqual(result["requestId"], "req-6")
        self.assertTrue(result["success"])
        self.assertTrue(result["changed"])
        mock_winreg.SetValueEx.assert_not_called()

    def test_apply_config_validates_inputs(self):
        plugin.winreg = MagicMock()

        # Invalid CompressionLevel type
        res = plugin.apply_config({"settings": {"CompressionLevel": "high"}}, {}, "req-7")
        self.assertFalse(res["success"])
        self.assertIn("CompressionLevel", res["error"])

        # Invalid CompressionLevel range
        res = plugin.apply_config({"settings": {"CompressionLevel": 10}}, {}, "req-8")
        self.assertFalse(res["success"])
        self.assertIn("CompressionLevel", res["error"])

        # Invalid CompressionMethod type
        res = plugin.apply_config({"settings": {"CompressionMethod": 123}}, {}, "req-9")
        self.assertFalse(res["success"])
        self.assertIn("CompressionMethod", res["error"])

        # Invalid EncryptHeaders type
        res = plugin.apply_config({"settings": {"EncryptHeaders": "True"}}, {}, "req-10")
        self.assertFalse(res["success"])
        self.assertIn("EncryptHeaders", res["error"])

        # Invalid ContextMenu type
        res = plugin.apply_config({"settings": {"ContextMenu": "style1"}}, {}, "req-11")
        self.assertFalse(res["success"])
        self.assertIn("ContextMenu", res["error"])

        # Invalid InstallDir type
        res = plugin.apply_config({"settings": {"InstallDir": ["C:/"]}}, {}, "req-12")
        self.assertFalse(res["success"])
        self.assertIn("InstallDir", res["error"])

    def test_apply_config_handles_write_errors(self):
        mock_winreg = MagicMock()
        mock_winreg.OpenKey.side_effect = FileNotFoundError()
        mock_winreg.CreateKeyEx.side_effect = PermissionError("Access denied")
        plugin.winreg = mock_winreg

        res = plugin.apply_config(
            {
                "settings": {
                    "CompressionLevel": 9,
                }
            },
            {"dryRun": False},
            "req-13",
        )

        self.assertFalse(res["success"])
        self.assertIn("Access denied", res["error"])

    def test_apply_config_handles_missing_winreg_module(self):
        plugin.winreg = None

        res = plugin.apply_config(
            {
                "settings": {
                    "CompressionLevel": 9,
                }
            },
            {"dryRun": False},
            "req-14",
        )

        self.assertFalse(res["success"])
        self.assertIn("winreg module not available", res["error"])

    def test_main_empty_input(self):
        import io
        import json

        with patch("sys.stdin", io.StringIO("")), patch("sys.stdout", new_callable=io.StringIO) as mock_stdout:
            plugin.main()
            output = json.loads(mock_stdout.getvalue().strip())
            self.assertEqual(output["requestId"], "unknown")
            self.assertFalse(output["success"])
            self.assertIn("Empty input", output["error"])

    def test_main_invalid_json(self):
        import io
        import json

        with (
            patch("sys.stdin", io.StringIO("{invalid json")),
            patch("sys.stdout", new_callable=io.StringIO) as mock_stdout,
        ):
            plugin.main()
            output = json.loads(mock_stdout.getvalue().strip())
            self.assertEqual(output["requestId"], "unknown")
            self.assertFalse(output["success"])
            self.assertIn("Failed to parse JSON request", output["error"])

    def test_read_settings_registry_corruption_triggers_backup(self):
        mock_winreg = MagicMock()
        mock_winreg.OpenKey.side_effect = Exception("Registry corruption error")
        plugin.winreg = mock_winreg

        with patch("subprocess.run") as mock_run:
            settings = plugin.read_settings()
            self.assertEqual(settings, {})
            mock_run.assert_called_once()
            args = mock_run.call_args[0][0]
            self.assertEqual(args[0], "reg.exe")
            self.assertEqual(args[1], "export")
            self.assertEqual(args[2], "HKCU\\Software\\7-Zip")


if __name__ == "__main__":
    unittest.main()
