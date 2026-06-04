import os
import sys
import unittest
from unittest.mock import mock_open, patch

_src_path = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "src"))
sys.path.append(_src_path)
import plugin

sys.path.remove(_src_path)


class TestRclonePlugin(unittest.TestCase):
    def test_parse_and_serialize_ini(self):
        text = "tpslimit = 10\n\n[drive]\ntype = drive\n# comment\nscope = drive.file\n"
        blocks, has_newline, is_crlf = plugin.parse_ini(text)
        self.assertEqual(len(blocks), 2)
        self.assertIsNone(blocks[0]["name"])
        self.assertEqual(blocks[1]["name"], "drive")

        output = plugin.serialize_ini(blocks, has_newline, is_crlf)
        self.assertEqual(output, text)

    def test_merge_settings(self):
        text = "[drive]\ntype = drive\n"
        blocks, has_newline, is_crlf = plugin.parse_ini(text)

        args = {
            "remotes": {
                "drive": {
                    "scope": "drive.file",
                    "type": "drive",  # unchanged
                },
                "s3": {"type": "s3"},
            },
            "settings": {"tpslimit": 20},
        }

        changed = plugin.merge_settings(blocks, args)
        self.assertTrue(changed)

        output = plugin.serialize_ini(blocks, has_newline, is_crlf)

        # Expect settings at the top, existing drive remote updated, and new s3 remote added.
        expected = "tpslimit = 20\n[drive]\ntype = drive\nscope = drive.file\n\n[s3]\ntype = s3\n"
        self.assertEqual(output, expected)

    def test_merge_no_change(self):
        text = "tpslimit = 10\n\n[drive]\ntype = drive\n"
        blocks, has_newline, is_crlf = plugin.parse_ini(text)

        args = {
            "remotes": {"drive": {"type": "drive"}},
            "settings": {"tpslimit": 10},
        }

        changed = plugin.merge_settings(blocks, args)
        self.assertFalse(changed)

    @patch("plugin.shutil.which")
    @patch("plugin.os.path.exists")
    def test_check_installed_via_path(self, mock_exists, mock_which):
        mock_which.return_value = "C:\\path\\rclone.exe"
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
    @patch("plugin.read_text")
    @patch("plugin.write_text")
    def test_apply_config_real_run(self, mock_write, mock_read, mock_get_path):
        mock_get_path.return_value = "dummy.conf"
        mock_read.return_value = "[drive]\ntype = drive\n"

        args = {"settings": {"tpslimit": 10}}
        context = {"dryRun": False}

        res = plugin.apply_config(args, context, "req-3")
        self.assertTrue(res["success"])
        self.assertTrue(res["changed"])
        self.assertEqual(res["requestId"], "req-3")
        mock_write.assert_called_once_with("dummy.conf", "tpslimit = 10\n[drive]\ntype = drive\n")

    @patch("plugin.get_config_path")
    @patch("plugin.read_text")
    @patch("plugin.write_text")
    def test_apply_config_dry_run(self, mock_write, mock_read, mock_get_path):
        mock_get_path.return_value = "dummy.conf"
        mock_read.return_value = "[drive]\ntype = drive\n"

        args = {"settings": {"tpslimit": 10}}
        context = {"dryRun": True}

        res = plugin.apply_config(args, context, "req-4")
        self.assertTrue(res["success"])
        self.assertTrue(res["changed"])
        mock_write.assert_not_called()

    @patch("plugin.os.makedirs")
    @patch("plugin.os.replace")
    def test_write_text(self, mock_replace, mock_makedirs):
        file_path = "dummy/path.conf"
        data = "tpslimit = 10\n"
        m_open = mock_open()
        with patch("plugin.open", m_open):
            plugin.write_text(file_path, data)

        mock_makedirs.assert_called_once_with("dummy", mode=0o700, exist_ok=True)
        m_open.assert_called_once_with(file_path + ".tmp", "w", encoding="utf-8")
        handle = m_open()
        handle.write.assert_called_with(data)
        mock_replace.assert_called_once_with(file_path + ".tmp", file_path)


if __name__ == "__main__":
    unittest.main()
