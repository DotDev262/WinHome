import os
import sys
import tempfile
import unittest
from unittest.mock import patch

sys.path.append(os.path.join(os.path.dirname(__file__), "..", "src"))
import plugin


class TestCheckInstalled(unittest.TestCase):
    def test_sdkman_installed(self):
        with tempfile.TemporaryDirectory() as tmpdir:
            sdkman_dir = os.path.join(tmpdir, ".sdkman")

            os.makedirs(sdkman_dir)

            with patch.dict(
                os.environ,
                {"USERPROFILE": tmpdir},
                clear=False,
            ):
                result = plugin.check_installed({}, "req-1")

        self.assertTrue(result["success"])
        self.assertTrue(result["data"])

    def test_sdkman_not_installed(self):
        with tempfile.TemporaryDirectory() as tmpdir:
            with patch.dict(
                os.environ,
                {"USERPROFILE": tmpdir},
                clear=False,
            ):
                result = plugin.check_installed({}, "req-2")

        self.assertTrue(result["success"])
        self.assertFalse(result["data"])


class TestApplyConfig(unittest.TestCase):
    def test_creates_file_if_not_exists(self):
        with tempfile.TemporaryDirectory() as tmpdir:
            config_path = os.path.join(
                tmpdir,
                ".sdkman",
                "etc",
                "config",
            )

            with patch(
                "plugin.get_config_path",
                return_value=config_path,
            ):
                result = plugin.apply_config(
                    {"settings": {"sdkman_auto_answer": True}},
                    {},
                    "req-10",
                )

            self.assertTrue(result["success"])
            self.assertTrue(result["changed"])
            self.assertTrue(os.path.exists(config_path))

    def test_no_change_when_same(self):
        with tempfile.TemporaryDirectory() as tmpdir:
            config_path = os.path.join(
                tmpdir,
                ".sdkman",
                "etc",
                "config",
            )

            os.makedirs(os.path.dirname(config_path))

            with open(config_path, "w") as f:
                f.write("sdkman_auto_answer=true\n")

            with patch(
                "plugin.get_config_path",
                return_value=config_path,
            ):
                result = plugin.apply_config(
                    {"settings": {"sdkman_auto_answer": True}},
                    {},
                    "req-11",
                )

            self.assertTrue(result["success"])
            self.assertFalse(result["changed"])

    def test_dry_run_does_not_write(self):
        with tempfile.TemporaryDirectory() as tmpdir:
            config_path = os.path.join(
                tmpdir,
                ".sdkman",
                "etc",
                "config",
            )

            with patch(
                "plugin.get_config_path",
                return_value=config_path,
            ):
                result = plugin.apply_config(
                    {"settings": {"sdkman_auto_answer": True}},
                    {"dryRun": True},
                    "req-12",
                )

            self.assertTrue(result["success"])
            self.assertTrue(result["changed"])
            self.assertFalse(os.path.exists(config_path))

    def test_invalid_settings_type(self):
        with self.assertRaisesRegex(
            ValueError,
            "settings must be an object",
        ):
            plugin.apply_config(
                {"settings": "invalid"},
                {},
                "req-invalid",
            )

    def test_idempotent(self):
        with tempfile.TemporaryDirectory() as tmpdir:
            config_path = os.path.join(
                tmpdir,
                ".sdkman",
                "etc",
                "config",
            )

            settings = {"settings": {"sdkman_auto_answer": True}}

            with patch(
                "plugin.get_config_path",
                return_value=config_path,
            ):
                plugin.apply_config(
                    settings,
                    {},
                    "req-13",
                )

                result2 = plugin.apply_config(
                    settings,
                    {},
                    "req-14",
                )

            self.assertTrue(result2["success"])
            self.assertFalse(result2["changed"])

    def test_preserves_unknown_keys(self):
        with tempfile.TemporaryDirectory() as tmpdir:
            config_path = os.path.join(
                tmpdir,
                ".sdkman",
                "etc",
                "config",
            )

            os.makedirs(os.path.dirname(config_path))

            with open(config_path, "w") as f:
                f.write("custom_key=keepme\nsdkman_auto_answer=false\n")

            with patch(
                "plugin.get_config_path",
                return_value=config_path,
            ):
                plugin.apply_config(
                    {"settings": {"sdkman_auto_answer": True}},
                    {},
                    "req-test",
                )

            with open(config_path, "r") as f:
                contents = f.read()

            self.assertIn("custom_key=keepme", contents)
            self.assertIn(
                "sdkman_auto_answer=true",
                contents,
            )

    def test_key_value_parsing(self):
        with tempfile.TemporaryDirectory() as tmpdir:
            config_path = os.path.join(
                tmpdir,
                ".sdkman",
                "etc",
                "config",
            )

            os.makedirs(os.path.dirname(config_path))

            with open(config_path, "w") as f:
                f.write("sdkman_auto_answer=false\nsdkman_beta_channel=true\n")

            config = plugin.read_config(config_path)

            self.assertEqual(
                config["sdkman_auto_answer"],
                "false",
            )

            self.assertEqual(
                config["sdkman_beta_channel"],
                "true",
            )

    def test_none_values_are_ignored(self):
        target = {}

        changed = plugin.merge_settings(
            target,
            {"sdkman_auto_answer": None},
        )

        self.assertFalse(changed)
        self.assertEqual(target, {})


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
        with tempfile.TemporaryDirectory() as tmpdir:
            os.makedirs(os.path.join(tmpdir, ".sdkman"))

            with patch.dict(
                os.environ,
                {"USERPROFILE": tmpdir},
                clear=False,
            ):
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
