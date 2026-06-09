#!/usr/bin/env python3
# /// script
# dependencies = [
#   "pyyaml",
# ]
# ///

import json
import os
import sys
import tempfile
import unittest
from io import StringIO
from unittest.mock import patch

import yaml

sys.path.insert(0, os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "src")))
import plugin


class TestGhPlugin(unittest.TestCase):
    def run_main(self, payload: dict) -> dict:
        stdin = StringIO(json.dumps(payload) + "\n")
        stdout = StringIO()
        with patch("sys.stdin", stdin), patch("sys.stdout", stdout):
            plugin.main()
        return json.loads(stdout.getvalue().strip())

    def test_check_installed_returns_true_when_gh_is_found(self):
        with patch(
            "plugin.shutil.which",
            side_effect=lambda name: "C:/Program Files/gh/gh.exe" if name in {"gh", "gh.exe"} else None,
        ):
            response = self.run_main({"requestId": "req-1", "command": "check_installed", "args": {}})

        self.assertEqual(response["requestId"], "req-1")
        self.assertTrue(response["success"])
        self.assertFalse(response["changed"])
        self.assertTrue(response["data"])

    def test_check_installed_returns_false_when_gh_is_missing(self):
        with patch("plugin.shutil.which", return_value=None):
            response = self.run_main({"requestId": "req-2", "command": "check_installed", "args": {}})

        self.assertEqual(response["requestId"], "req-2")
        self.assertTrue(response["success"])
        self.assertFalse(response["changed"])
        self.assertFalse(response["data"])

    def test_apply_writes_merged_config_and_returns_changed_true(self):
        with tempfile.TemporaryDirectory() as tmp_dir:
            config_dir = os.path.join(tmp_dir, "GitHub CLI")
            config_path = os.path.join(config_dir, "config.yml")
            os.makedirs(config_dir, exist_ok=True)
            with open(config_path, "w", encoding="utf-8") as file_handle:
                yaml.dump(
                    {"prompt": "disabled", "existing": "keep"},
                    file_handle,
                    default_flow_style=False,
                    sort_keys=False,
                )

            with patch("plugin.get_config_path", return_value=config_path):
                response = self.run_main(
                    {
                        "requestId": "req-3",
                        "command": "apply",
                        "args": {
                            "git_protocol": "https",
                            "editor": "code --wait",
                            "prompt": "enabled",
                            "pager": "less",
                            "http_unix_socket": "",
                            "browser": "",
                        },
                        "context": {"dryRun": False},
                    }
                )

            self.assertEqual(response["requestId"], "req-3")
            self.assertTrue(response["success"])
            self.assertTrue(response["changed"])

            with open(config_path, "r", encoding="utf-8") as file_handle:
                content = yaml.safe_load(file_handle)

            self.assertEqual(content["git_protocol"], "https")
            self.assertEqual(content["editor"], "code --wait")
            self.assertEqual(content["prompt"], "enabled")
            self.assertEqual(content["pager"], "less")
            self.assertNotIn("http_unix_socket", content)
            self.assertNotIn("browser", content)
            self.assertEqual(content["existing"], "keep")

    def test_apply_with_no_changes_returns_changed_false(self):
        with tempfile.TemporaryDirectory() as tmp_dir:
            config_dir = os.path.join(tmp_dir, "GitHub CLI")
            config_path = os.path.join(config_dir, "config.yml")
            os.makedirs(config_dir, exist_ok=True)
            initial_content = {
                "git_protocol": "https",
                "editor": "code --wait",
                "prompt": "enabled",
                "pager": "less",
            }
            with open(config_path, "w", encoding="utf-8") as file_handle:
                yaml.dump(
                    initial_content,
                    file_handle,
                    default_flow_style=False,
                    sort_keys=False,
                )

            with patch("plugin.get_config_path", return_value=config_path):
                response = self.run_main(
                    {
                        "requestId": "req-4",
                        "command": "apply",
                        "args": {
                            "git_protocol": "https",
                            "editor": "code --wait",
                            "prompt": "enabled",
                            "pager": "less",
                            "http_unix_socket": "",
                            "browser": "",
                        },
                        "context": {"dryRun": False},
                    }
                )

            self.assertEqual(response["requestId"], "req-4")
            self.assertTrue(response["success"])
            self.assertFalse(response["changed"])

            with open(config_path, "r", encoding="utf-8") as file_handle:
                content = yaml.safe_load(file_handle)

            self.assertEqual(content, initial_content)

    def test_apply_with_dry_run_does_not_write_file(self):
        with tempfile.TemporaryDirectory() as tmp_dir:
            config_path = os.path.join(tmp_dir, "GitHub CLI", "config.yml")

            with patch("plugin.get_config_path", return_value=config_path):
                response = self.run_main(
                    {
                        "requestId": "req-5",
                        "command": "apply",
                        "args": {"git_protocol": "https", "dry_run": True},
                        "context": {"dryRun": True},
                    }
                )

            self.assertEqual(response["requestId"], "req-5")
            self.assertTrue(response["success"])
            self.assertTrue(response["changed"])
            self.assertFalse(os.path.exists(config_path))

    def test_apply_creates_missing_directory(self):
        with tempfile.TemporaryDirectory() as tmp_dir:
            config_path = os.path.join(tmp_dir, "GitHub CLI", "config.yml")
            self.assertFalse(os.path.exists(os.path.dirname(config_path)))

            with patch("plugin.get_config_path", return_value=config_path):
                response = self.run_main(
                    {
                        "requestId": "req-6",
                        "command": "apply",
                        "args": {"git_protocol": "https"},
                        "context": {"dryRun": False},
                    }
                )

            self.assertEqual(response["requestId"], "req-6")
            self.assertTrue(response["success"])
            self.assertTrue(response["changed"])
            self.assertTrue(os.path.isdir(os.path.dirname(config_path)))
            self.assertTrue(os.path.exists(config_path))

    def test_apply_returns_error_when_pyyaml_is_missing(self):
        with patch.object(plugin, "yaml", None):
            response = self.run_main(
                {
                    "requestId": "req-7",
                    "command": "apply",
                    "args": {"git_protocol": "https"},
                    "context": {"dryRun": False},
                }
            )

        self.assertEqual(response["requestId"], "req-7")
        self.assertFalse(response["success"])
        self.assertFalse(response["changed"])
        self.assertIn("PyYAML", response["error"])

    def test_unknown_command_returns_error(self):
        response = self.run_main(
            {
                "requestId": "req-8",
                "command": "explode",
                "args": {},
                "context": {},
            }
        )
        self.assertEqual(response["requestId"], "req-8")
        self.assertFalse(response["success"])
        self.assertFalse(response["changed"])
        self.assertIn("Unknown command", response["error"])


if __name__ == "__main__":
    unittest.main()
