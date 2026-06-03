import os
import sys
import unittest
from unittest.mock import mock_open, patch

# Append src to sys.path and remove it after import to avoid side effects
_src_path = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "src"))
sys.path.append(_src_path)
import plugin

sys.path.remove(_src_path)


class TestShareXPlugin(unittest.TestCase):
    def test_deep_merge(self):
        target = {
            "ApplicationSettings": {
                "ShowTray": False,
                "CaptureSettings": {"Screenshot": {"CaptureTransparency": True}},
            },
            "ImageSettings": {"ImageFormat": "JPEG"},
        }
        source = {
            "ApplicationSettings": {
                "ShowTray": True,
                "CaptureSettings": {"Screenshot": {"CaptureTransparency": False}},
            },
            "UploadSettings": {"DestinationType": "Imgur"},
        }
        changed = plugin.deep_merge(target, source)
        self.assertTrue(changed)
        self.assertTrue(target["ApplicationSettings"]["ShowTray"])
        self.assertFalse(target["ApplicationSettings"]["CaptureSettings"]["Screenshot"]["CaptureTransparency"])
        self.assertEqual(target["ImageSettings"]["ImageFormat"], "JPEG")
        self.assertEqual(target["UploadSettings"]["DestinationType"], "Imgur")

    def test_deep_merge_no_change(self):
        target = {"key": "val"}
        source = {"key": "val"}
        changed = plugin.deep_merge(target, source)
        self.assertFalse(changed)
        self.assertEqual(target, {"key": "val"})

    @patch("plugin.shutil.which")
    @patch("plugin.os.path.exists")
    def test_check_installed_via_path(self, mock_exists, mock_which):
        mock_which.return_value = "C:\\path\\ShareX.exe"
        res = plugin.check_installed({}, "req-1")
        self.assertTrue(res["success"])
        self.assertTrue(res["data"])

    @patch("plugin.shutil.which")
    @patch("plugin.os.path.exists")
    def test_check_installed_via_program_files(self, mock_exists, mock_which):
        mock_which.return_value = None
        mock_exists.return_value = True
        res = plugin.check_installed({}, "req-2")
        self.assertTrue(res["success"])
        self.assertTrue(res["data"])

    @patch("plugin.get_config_path")
    @patch("plugin.read_json")
    @patch("plugin.write_json")
    def test_apply_config_real_run(self, mock_write, mock_read, mock_get_path):
        mock_get_path.return_value = "dummy.json"
        mock_read.return_value = {"a": 1}

        args = {"settings": {"a": 2, "b": 3}}
        context = {"dryRun": False}

        res = plugin.apply_config(args, context, "req-3")
        self.assertTrue(res["success"])
        self.assertTrue(res["changed"])
        self.assertEqual(res["requestId"], "req-3")
        mock_write.assert_called_once_with("dummy.json", {"a": 2, "b": 3})

    @patch("plugin.get_config_path")
    @patch("plugin.read_json")
    @patch("plugin.write_json")
    def test_apply_config_dry_run(self, mock_write, mock_read, mock_get_path):
        mock_get_path.return_value = "dummy.json"
        mock_read.return_value = {"a": 1}

        args = {"settings": {"a": 2}}
        context = {"dryRun": True}

        res = plugin.apply_config(args, context, "req-4")
        self.assertTrue(res["success"])
        self.assertTrue(res["changed"])
        mock_write.assert_not_called()

    @patch("plugin.get_config_path")
    @patch("plugin.read_json")
    @patch("plugin.write_json")
    def test_apply_config_no_change(self, mock_write, mock_read, mock_get_path):
        mock_get_path.return_value = "dummy.json"
        mock_read.return_value = {"a": 1}

        args = {"settings": {"a": 1}}
        context = {"dryRun": False}

        res = plugin.apply_config(args, context, "req-5")
        self.assertTrue(res["success"])
        self.assertFalse(res["changed"])
        mock_write.assert_not_called()

    @patch("plugin.os.makedirs")
    @patch("plugin.os.replace")
    def test_write_json(self, mock_replace, mock_makedirs):
        file_path = "dummy/path.json"
        data = {"a": 1}
        m_open = mock_open()
        with patch("plugin.open", m_open):
            plugin.write_json(file_path, data)

        mock_makedirs.assert_called_once_with("dummy", mode=0o700, exist_ok=True)
        m_open.assert_called_once_with(file_path + ".tmp", "w", encoding="utf-8")
        handle = m_open()
        handle.write.assert_called_with("\n")
        mock_replace.assert_called_once_with(file_path + ".tmp", file_path)

    @patch("plugin.get_config_path")
    def test_read_corrupted_config(self, mock_get_path):
        import json
        import tempfile

        with tempfile.TemporaryDirectory() as temp_dir:
            config_path = os.path.join(temp_dir, "ShareX.json")
            mock_get_path.return_value = config_path

            # Write corrupted config
            with open(config_path, "w", encoding="utf-8") as f:
                f.write("{ invalid json")

            args = {"settings": {"a": 2}}
            context = {"dryRun": False}

            # Should back up corrupted and apply new
            res = plugin.apply_config(args, context, "req-5")
            self.assertTrue(res["success"])
            self.assertTrue(res["changed"])

            # Verify file WAS reset and written
            with open(config_path, "r", encoding="utf-8") as f:
                content = json.load(f)
            self.assertEqual(content["a"], 2)

            # Verify backup was created
            backups = [f for f in os.listdir(temp_dir) if f.startswith("ShareX.json.corrupted.")]
            self.assertEqual(len(backups), 1)


if __name__ == "__main__":
    unittest.main()
