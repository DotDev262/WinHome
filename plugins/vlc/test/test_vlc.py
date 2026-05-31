import os
import sys
import unittest
from unittest.mock import patch

_src_path = os.path.abspath(
    os.path.join(os.path.dirname(__file__), "..", "src")
)
sys.path.append(_src_path)
try:
    import plugin
finally:
    sys.path.remove(_src_path)


class TestVlcPlugin(unittest.TestCase):
    def test_parse_and_serialize_vlcrc(self):
        text = "# vlcrc\nvolume=128\nnetwork-caching=1000\n[foo]\nbar=1\n"
        blocks, has_newline, is_crlf = plugin.parse_ini(text)
        self.assertEqual(len(blocks), 2)
        self.assertIsNone(blocks[0]["name"])
        self.assertEqual(blocks[1]["name"], "foo")
        output = plugin.serialize_ini(blocks, has_newline, is_crlf)
        self.assertEqual(output, text)

    def test_merge_global_settings(self):
        text = "volume=128\n"
        blocks, has_newline, is_crlf = plugin.parse_ini(text)
        changed = plugin.merge_settings(
            blocks,
            {"volume": 256, "network-caching": 400},
        )
        self.assertTrue(changed)
        output = plugin.serialize_ini(blocks, has_newline, is_crlf)
        self.assertIn("volume=256", output)
        self.assertIn("network-caching=400", output)

    def test_merge_multi_value_key(self):
        text = "enable-lua-sd=old\n"
        blocks, has_newline, is_crlf = plugin.parse_ini(text)
        changed = plugin.merge_settings(
            blocks,
            {"enable-lua-sd": ["bonjour", "podcast"]},
        )
        self.assertTrue(changed)
        output = plugin.serialize_ini(blocks, has_newline, is_crlf)
        self.assertEqual(
            output,
            "enable-lua-sd=bonjour\nenable-lua-sd=podcast\n",
        )

    def test_merge_section_settings(self):
        text = "[core]\nexisting=1\n"
        blocks, has_newline, is_crlf = plugin.parse_ini(text)
        changed = plugin.merge_settings(
            blocks,
            {"core": {"volume": 200, "existing": 1}},
        )
        self.assertTrue(changed)
        output = plugin.serialize_ini(blocks, has_newline, is_crlf)
        self.assertIn("[core]", output)
        self.assertIn("volume=200", output)

    def test_merge_no_change(self):
        text = "volume=256\n"
        blocks, has_newline, is_crlf = plugin.parse_ini(text)
        changed = plugin.merge_settings(blocks, {"volume": 256})
        self.assertFalse(changed)

    def test_merge_bool_as_vlc_int(self):
        text = ""
        blocks, has_newline, is_crlf = plugin.parse_ini(text)
        changed = plugin.merge_settings(
            blocks,
            {"video-on-top": True, "playlist-cork": False},
        )
        self.assertTrue(changed)
        output = plugin.serialize_ini(blocks, True, is_crlf)
        self.assertIn("video-on-top=1", output)
        self.assertIn("playlist-cork=0", output)

    @patch("plugin.os.getenv")
    @patch("plugin.shutil.which")
    @patch("plugin.os.path.isdir")
    def test_check_installed_via_appdata(self, mock_isdir, mock_which, mock_getenv):
        mock_getenv.side_effect = (
            lambda key, default=None: "C:\\Users\\me\\AppData\\Roaming"
            if key == "APPDATA"
            else default
        )
        mock_isdir.return_value = True
        mock_which.return_value = None
        res = plugin.check_installed({}, "req-1")
        self.assertTrue(res["success"])
        self.assertTrue(res["data"])

    @patch("plugin.shutil.which")
    @patch("plugin.os.path.isdir")
    def test_check_installed_via_path(self, mock_isdir, mock_which):
        mock_isdir.return_value = False
        mock_which.side_effect = lambda name: "C:\\VLC\\vlc.exe" if name == "vlc.exe" else None
        res = plugin.check_installed({}, "req-2")
        self.assertTrue(res["success"])
        self.assertTrue(res["data"])

    @patch("plugin.shutil.which")
    @patch("plugin.os.path.isdir")
    def test_check_installed_absent(self, mock_isdir, mock_which):
        mock_isdir.return_value = False
        mock_which.return_value = None
        res = plugin.check_installed({}, "req-3")
        self.assertTrue(res["success"])
        self.assertFalse(res["data"])

    @patch("plugin.get_config_path")
    @patch("plugin.read_text")
    @patch("plugin.write_text")
    def test_apply_config_writes_file(self, mock_write, mock_read, mock_get_path):
        mock_get_path.return_value = "dummy\\vlcrc"
        mock_read.return_value = "volume=128\n"

        res = plugin.apply_config(
            {"settings": {"volume": 256}},
            {"dryRun": False},
            "req-4",
        )
        self.assertTrue(res["success"])
        self.assertTrue(res["changed"])
        mock_write.assert_called_once()
        written = mock_write.call_args[0][1]
        self.assertIn("volume=256", written)

    @patch("plugin.get_config_path")
    @patch("plugin.read_text")
    @patch("plugin.write_text")
    def test_apply_config_dry_run(self, mock_write, mock_read, mock_get_path):
        mock_get_path.return_value = "dummy\\vlcrc"
        mock_read.return_value = "volume=128\n"

        res = plugin.apply_config(
            {"settings": {"volume": 256}},
            {"dryRun": True},
            "req-5",
        )
        self.assertTrue(res["success"])
        self.assertTrue(res["changed"])
        mock_write.assert_not_called()

    @patch("plugin.get_config_path")
    @patch("plugin.read_text")
    @patch("plugin.write_text")
    def test_apply_config_creates_when_missing(self, mock_write, mock_read, mock_get_path):
        mock_get_path.return_value = "dummy\\vlcrc"
        mock_read.return_value = ""

        res = plugin.apply_config(
            {"settings": {"snapshot-format": "png"}},
            {"dryRun": False},
            "req-6",
        )
        self.assertTrue(res["success"])
        self.assertTrue(res["changed"])
        written = mock_write.call_args[0][1]
        self.assertIn("snapshot-format=png", written)


if __name__ == "__main__":
    unittest.main()
