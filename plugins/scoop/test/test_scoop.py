import os
import sys
import unittest
from unittest.mock import patch

_src_path = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "src"))
try:
    sys.path.append(_src_path)
    import plugin
finally:
    sys.path.remove(_src_path)


class TestScoopPlugin(unittest.TestCase):
    def test_merge_settings_creates_new(self):
        target = {}
        source = {"root_path": "D:\\scoop"}
        changed = plugin.merge_settings(target, source)
        self.assertTrue(changed)
        self.assertEqual(target["root_path"], "D:\\scoop")

    def test_merge_settings_updates_existing(self):
        target = {"root_path": "C:\\scoop", "aria2-enabled": False}
        source = {"root_path": "D:\\scoop", "debug": True}
        changed = plugin.merge_settings(target, source)
        self.assertTrue(changed)
        self.assertEqual(target["root_path"], "D:\\scoop")
        self.assertEqual(target["aria2-enabled"], False)
        self.assertEqual(target["debug"], True)

    def test_merge_settings_no_change(self):
        target = {"root_path": "D:\\scoop"}
        source = {"root_path": "D:\\scoop"}
        changed = plugin.merge_settings(target, source)
        self.assertFalse(changed)
        self.assertEqual(target["root_path"], "D:\\scoop")

    @patch("shutil.which")
    def test_check_installed_true(self, mock_which):
        mock_which.return_value = "C:\\scoop\\shims\\scoop.cmd"
        res = plugin.check_installed({}, "req-1")
        self.assertTrue(res["success"])
        self.assertTrue(res["data"])

    @patch("shutil.which")
    def test_check_installed_false(self, mock_which):
        mock_which.return_value = None
        res = plugin.check_installed({}, "req-2")
        self.assertTrue(res["success"])
        self.assertFalse(res["data"])

    @patch("os.getenv")
    def test_get_config_path_with_xdg(self, mock_getenv):
        def side_effect(key):
            if key == "XDG_CONFIG_HOME":
                return "C:\\xdg"
            if key == "USERPROFILE":
                return "C:\\Users\\test"
            return None

        mock_getenv.side_effect = side_effect

        path = plugin.get_config_path()
        self.assertEqual(path, os.path.join("C:\\xdg", "scoop", "config.json"))

    @patch("os.getenv")
    def test_get_config_path_fallback_userprofile(self, mock_getenv):
        def side_effect(key):
            if key == "XDG_CONFIG_HOME":
                return None
            if key == "USERPROFILE":
                return "C:\\Users\\test"
            return None

        mock_getenv.side_effect = side_effect

        path = plugin.get_config_path()
        self.assertEqual(
            path,
            os.path.join("C:\\Users\\test", ".config", "scoop", "config.json"),
        )

    @patch("plugin.read_json")
    @patch("plugin.write_json")
    @patch("plugin.get_config_path")
    def test_apply_config_success(self, mock_path, mock_write, mock_read):
        mock_path.return_value = "dummy.json"
        mock_read.return_value = {"aria2-enabled": False}

        args = {"settings": {"aria2-enabled": True}}
        res = plugin.apply_config(args, {}, "req-3")

        self.assertTrue(res["success"])
        self.assertTrue(res["changed"])
        mock_write.assert_called_once_with("dummy.json", {"aria2-enabled": True})

    @patch("plugin.read_json")
    @patch("plugin.write_json")
    @patch("plugin.get_config_path")
    def test_apply_config_dry_run(self, mock_path, mock_write, mock_read):
        mock_path.return_value = "dummy.json"
        mock_read.return_value = {}

        args = {"settings": {"root_path": "D:\\scoop"}}
        context = {"dryRun": True}
        res = plugin.apply_config(args, context, "req-4")

        self.assertTrue(res["success"])
        self.assertTrue(res["changed"])
        mock_write.assert_not_called()

    @patch("plugin.read_json")
    @patch("plugin.write_json")
    @patch("plugin.get_config_path")
    def test_apply_config_no_changes(self, mock_path, mock_write, mock_read):
        mock_path.return_value = "dummy.json"
        mock_read.return_value = {"root_path": "D:\\scoop"}

        args = {"settings": {"root_path": "D:\\scoop"}}
        res = plugin.apply_config(args, {}, "req-5")

        self.assertTrue(res["success"])
        self.assertFalse(res["changed"])
        mock_write.assert_not_called()

    @patch("os.path.exists")
    @patch(
        "builtins.open",
        new_callable=unittest.mock.mock_open,
        read_data="{invalid_json",
    )
    @patch("shutil.move")
    def test_read_json_corrupted_backup(self, mock_move, mock_file, mock_exists):
        mock_exists.return_value = True

        # Test JSONDecodeError path
        data = plugin.read_json("dummy.json")
        self.assertEqual(data, {})
        mock_move.assert_called_once()
        args, _ = mock_move.call_args
        self.assertEqual(args[0], "dummy.json")
        self.assertIn("dummy.json.corrupted.", args[1])

    @patch("os.path.exists")
    @patch("builtins.open")
    @patch("shutil.move")
    def test_read_json_oserror_backup(self, mock_move, mock_file, mock_exists):
        mock_exists.return_value = True
        mock_file.side_effect = OSError("Access Denied")

        # Test OSError path
        data = plugin.read_json("dummy.json")
        self.assertEqual(data, {})
        mock_move.assert_called_once()
        args, _ = mock_move.call_args
        self.assertEqual(args[0], "dummy.json")
        self.assertIn("dummy.json.corrupted.", args[1])

    @patch("plugin.tempfile.mkstemp")
    @patch("plugin.os.replace")
    @patch("plugin.os.makedirs")
    @patch("plugin.os.fdopen")
    def test_write_json_uses_mkstemp(self, mock_fdopen, mock_makedirs, mock_replace, mock_mkstemp):
        mock_mkstemp.return_value = (5, "/tmp/scoop-abc123")
        mock_fdopen.return_value.__enter__ = lambda s: s
        mock_fdopen.return_value.__exit__ = lambda s, *a: False
        mock_fdopen.return_value.write = lambda x: None

        plugin.write_json("/fake/path/config.json", {"debug": True})

        mock_mkstemp.assert_called_once()
        call_kwargs = mock_mkstemp.call_args
        self.assertEqual(
            call_kwargs.kwargs.get("prefix") or call_kwargs[1].get("prefix"),
            "scoop-",
        )
        mock_replace.assert_called_once_with("/tmp/scoop-abc123", "/fake/path/config.json")

    def test_write_json_atomic_real(self):
        import json as js
        import tempfile as tf

        with tf.TemporaryDirectory() as tmpdir:
            config_path = os.path.join(tmpdir, "config.json")
            plugin.write_json(config_path, {"root_path": "D:\\scoop"})
            with open(config_path, "r") as f:
                data = js.load(f)
            self.assertEqual(data["root_path"], "D:\\scoop")
            # Ensure no leftover temp files
            leftover = [f for f in os.listdir(tmpdir) if f.startswith("scoop-")]
            self.assertEqual(leftover, [])


if __name__ == "__main__":
    unittest.main()
