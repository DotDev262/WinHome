import os
import sys
import tempfile
import unittest
from unittest.mock import patch

sys.path.append(os.path.join(os.path.dirname(__file__), "..", "src"))
import plugin


class TestCheckInstalled(unittest.TestCase):
    def test_cargo_in_path(self):
        with patch(
            "shutil.which",
            side_effect=lambda x: "/usr/bin/cargo" if x == "cargo" else None,
        ):
            result = plugin.check_installed({}, "req-1")
        self.assertTrue(result["success"])
        self.assertTrue(result["data"])

    def test_cargo_not_found(self):
        with patch("shutil.which", return_value=None):
            with patch.dict(os.environ, {"CARGO_HOME": ""}, clear=False):
                result = plugin.check_installed({}, "req-2")
        self.assertTrue(result["success"])
        self.assertFalse(result["data"])

    def test_cargo_via_cargo_home(self):
        with tempfile.TemporaryDirectory() as tmpdir:
            bin_dir = os.path.join(tmpdir, "bin")
            os.makedirs(bin_dir)
            cargo_exe = os.path.join(bin_dir, "cargo.exe")
            open(cargo_exe, "w").close()
            with patch("shutil.which", return_value=None):
                with patch.dict(os.environ, {"CARGO_HOME": tmpdir}):
                    result = plugin.check_installed({}, "req-3")
        self.assertTrue(result["success"])
        self.assertTrue(result["data"])


class TestTomlValue(unittest.TestCase):
    def test_bool_true(self):
        self.assertEqual(plugin.toml_value(True), "true")

    def test_bool_false(self):
        self.assertEqual(plugin.toml_value(False), "false")

    def test_int(self):
        self.assertEqual(plugin.toml_value(8), "8")

    def test_string(self):
        self.assertEqual(plugin.toml_value("always"), '"always"')

    def test_list(self):
        self.assertEqual(plugin.toml_value(["a", "b"]), '["a", "b"]')


class TestMergeSettings(unittest.TestCase):
    def test_simple_merge(self):
        target = {"http": {"timeout": 10}}
        source = {"http": {"timeout": 30, "proxy": "http://proxy:8080"}}
        changed = plugin.merge_settings(target, source)
        self.assertTrue(changed)
        self.assertEqual(target["http"]["timeout"], 30)
        self.assertEqual(target["http"]["proxy"], "http://proxy:8080")

    def test_no_change(self):
        target = {"build": {"jobs": 8}}
        source = {"build": {"jobs": 8}}
        changed = plugin.merge_settings(target, source)
        self.assertFalse(changed)

    def test_preserves_unmentioned_keys(self):
        target = {"net": {"retry": 3, "git-fetch-with-cli": True}}
        source = {"net": {"retry": 5}}
        plugin.merge_settings(target, source)
        self.assertEqual(target["net"]["git-fetch-with-cli"], True)
        self.assertEqual(target["net"]["retry"], 5)

    def test_deep_merge(self):
        target = {"registry": {"default": "crates-io"}}
        source = {"registry": {"default": "mirror"}}
        changed = plugin.merge_settings(target, source)
        self.assertTrue(changed)
        self.assertEqual(target["registry"]["default"], "mirror")


class TestApplyConfig(unittest.TestCase):
    def test_creates_file_if_not_exists(self):
        with tempfile.TemporaryDirectory() as tmpdir:
            config_path = os.path.join(tmpdir, ".cargo", "config.toml")
            with patch("plugin.get_config_path", return_value=config_path):
                result = plugin.apply_config({"settings": {"build": {"jobs": 4}}}, {}, "req-10")
            self.assertTrue(result["success"])
            self.assertTrue(result["changed"])
            self.assertTrue(os.path.exists(config_path))

    def test_no_change_when_same(self):
        with tempfile.TemporaryDirectory() as tmpdir:
            config_path = os.path.join(tmpdir, ".cargo", "config.toml")
            os.makedirs(os.path.dirname(config_path))
            with open(config_path, "w") as f:
                f.write("[build]\njobs = 4\n")
            with patch("plugin.get_config_path", return_value=config_path):
                result = plugin.apply_config({"settings": {"build": {"jobs": 4}}}, {}, "req-11")
            self.assertTrue(result["success"])
            self.assertFalse(result["changed"])

    def test_dry_run_does_not_write(self):
        with tempfile.TemporaryDirectory() as tmpdir:
            config_path = os.path.join(tmpdir, ".cargo", "config.toml")
            with patch("plugin.get_config_path", return_value=config_path):
                result = plugin.apply_config(
                    {"settings": {"term": {"color": "always"}}},
                    {"dryRun": True},
                    "req-12",
                )
            self.assertTrue(result["success"])
            self.assertTrue(result["changed"])
            self.assertFalse(os.path.exists(config_path))

    def test_idempotent(self):
        with tempfile.TemporaryDirectory() as tmpdir:
            config_path = os.path.join(tmpdir, ".cargo", "config.toml")
            settings = {"settings": {"net": {"retry": 5}}}
            with patch("plugin.get_config_path", return_value=config_path):
                plugin.apply_config(settings, {}, "req-13")
                result2 = plugin.apply_config(settings, {}, "req-14")
            self.assertTrue(result2["success"])
            self.assertFalse(result2["changed"])


class TestProtocol(unittest.TestCase):
    def test_unknown_command(self):
        result = plugin.handle(
            {
                "requestId": "req-20",
                "command": "unknown",
                "args": {},
                "context": {},
            }
        )
        self.assertFalse(result["success"])
        self.assertIn("Unknown command", result["error"])

    def test_check_installed_via_handle(self):
        with patch("shutil.which", return_value="/usr/bin/cargo"):
            result = plugin.handle(
                {
                    "requestId": "req-21",
                    "command": "check_installed",
                    "args": {},
                    "context": {},
                }
            )
        self.assertTrue(result["success"])
        self.assertEqual(result["requestId"], "req-21")


if __name__ == "__main__":
    unittest.main()
