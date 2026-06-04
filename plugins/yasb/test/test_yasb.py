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

_SRC_DIR = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "src"))
sys.path.append(_SRC_DIR)
try:
    import plugin
finally:
    sys.path.remove(_SRC_DIR)


class TestYasbPlugin(unittest.TestCase):
    def run_main(self, payload: dict) -> dict:
        stdin = StringIO(json.dumps(payload) + "\n")
        stdout = StringIO()

        with patch("sys.stdin", stdin), patch("sys.stdout", stdout):
            plugin.main()

        return json.loads(stdout.getvalue().strip())

    def test_check_installed_true_via_path(self):
        with patch("plugin.shutil.which", side_effect=lambda name: "C:/Tools/yasb.exe" if name == "yasb" else None):
            response = self.run_main({"requestId": "req-1", "command": "check_installed", "args": {}, "context": {}})

        self.assertTrue(response["success"])
        self.assertFalse(response["changed"])
        self.assertTrue(response["data"])

    def test_check_installed_true_via_config_dir(self):
        with tempfile.TemporaryDirectory() as tmp_dir:
            config_dir = os.path.join(tmp_dir, ".config", "yasb")
            os.makedirs(config_dir, exist_ok=True)

            with patch.dict(os.environ, {"USERPROFILE": tmp_dir}), patch("plugin.shutil.which", return_value=None):
                response = self.run_main(
                    {"requestId": "req-2", "command": "check_installed", "args": {}, "context": {}}
                )

        self.assertTrue(response["success"])
        self.assertTrue(response["data"])

    def test_apply_merges_bars_without_overwriting_existing_config(self):
        with tempfile.TemporaryDirectory() as tmp_dir:
            config_path = os.path.join(tmp_dir, ".config", "yasb", "config.yaml")
            initial = {
                "watch_stylesheet": False,
                "bars": {
                    "status-bar": {
                        "enabled": False,
                        "screens": ["*"],
                        "alignment": {"position": "bottom", "center": True},
                        "window_flags": {"always_on_top": True, "windows_app_bar": False},
                        "dimensions": {"width": "100%", "height": 30},
                        "padding": {"top": 2, "left": 8, "bottom": 2, "right": 8},
                        "widgets": {
                            "left": ["launcher"],
                            "center": ["date"],
                            "right": ["network"],
                        },
                    },
                    "secondary-bar": {
                        "enabled": True,
                        "widgets": {"left": ["custom"]},
                    },
                },
            }

            os.makedirs(os.path.dirname(config_path), exist_ok=True)

            with open(config_path, "w", encoding="utf-8") as file_handle:
                yaml.safe_dump(initial, file_handle, default_flow_style=False, sort_keys=False)

            payload = {
                "requestId": "req-3",
                "command": "apply",
                "args": {
                    "settings": {
                        "bars": {
                            "status-bar": {
                                "enabled": True,
                                "alignment": {"position": "top", "center": False},
                                "widgets": {
                                    "left": ["workspaces", "active_window"],
                                    "right": ["cpu", "memory", "volume", "battery"],
                                },
                            },
                            "music-bar": {
                                "enabled": True,
                                "screens": ["primary"],
                                "widgets": {"center": ["now_playing"]},
                            },
                        },
                        "watch_stylesheet": True,
                        "watch_config": True,
                        "debug": False,
                    },
                },
                "context": {"dryRun": False},
            }

            with patch.dict(os.environ, {"USERPROFILE": tmp_dir}):
                response = self.run_main(payload)

            self.assertTrue(response["success"])
            self.assertTrue(response["changed"])

            with open(config_path, "r", encoding="utf-8") as file_handle:
                content = yaml.safe_load(file_handle)

            self.assertTrue(content["watch_stylesheet"])
            self.assertTrue(content["watch_config"])
            self.assertFalse(content["debug"])
            self.assertTrue(content["bars"]["status-bar"]["enabled"])
            self.assertEqual(content["bars"]["status-bar"]["alignment"]["position"], "top")
            self.assertFalse(content["bars"]["status-bar"]["alignment"]["center"])
            self.assertEqual(content["bars"]["status-bar"]["widgets"]["left"], ["workspaces", "active_window"])
            self.assertEqual(content["bars"]["status-bar"]["widgets"]["center"], ["date"])
            self.assertEqual(content["bars"]["status-bar"]["widgets"]["right"], ["cpu", "memory", "volume", "battery"])
            self.assertIn("secondary-bar", content["bars"])
            self.assertIn("music-bar", content["bars"])

    def test_apply_dry_run_does_not_write(self):
        with tempfile.TemporaryDirectory() as tmp_dir:
            payload = {
                "requestId": "req-4",
                "command": "apply",
                "args": {"settings": {"bars": {"status-bar": {"enabled": True, "widgets": {"left": ["workspaces"]}}}}},
                "context": {"dryRun": True},
            }

            with patch.dict(os.environ, {"USERPROFILE": tmp_dir}):
                response = self.run_main(payload)

            config_path = os.path.join(tmp_dir, ".config", "yasb", "config.yaml")
            self.assertTrue(response["success"])
            self.assertTrue(response["changed"])
            self.assertTrue(response["data"]["wouldChange"])
            self.assertFalse(os.path.exists(config_path))

    def test_apply_returns_null_data_on_success(self):
        with tempfile.TemporaryDirectory() as tmp_dir:
            payload = {
                "requestId": "req-4b",
                "command": "apply",
                "args": {"settings": {"watch_config": True}},
                "context": {"dryRun": False},
            }

            with patch.dict(os.environ, {"USERPROFILE": tmp_dir}):
                response = self.run_main(payload)

            self.assertTrue(response["success"])
            self.assertIn("data", response)
            self.assertIsNone(response["data"])

    def test_apply_creates_missing_config_directory(self):
        with tempfile.TemporaryDirectory() as tmp_dir:
            payload = {
                "requestId": "req-5",
                "command": "apply",
                "args": {"settings": {"bars": {"status-bar": {"enabled": True, "widgets": {"left": ["workspaces"]}}}}},
                "context": {"dryRun": False},
            }

            with patch.dict(os.environ, {"USERPROFILE": tmp_dir}):
                response = self.run_main(payload)

            config_dir = os.path.join(tmp_dir, ".config", "yasb")
            config_path = os.path.join(config_dir, "config.yaml")

            self.assertTrue(response["success"])
            self.assertTrue(response["changed"])
            self.assertTrue(os.path.isdir(config_dir))
            self.assertTrue(os.path.exists(config_path))

    def test_apply_backups_corrupted_yaml_before_replacing(self):
        with tempfile.TemporaryDirectory() as tmp_dir:
            config_path = os.path.join(tmp_dir, ".config", "yasb", "config.yaml")
            os.makedirs(os.path.dirname(config_path), exist_ok=True)

            with open(config_path, "w", encoding="utf-8") as file_handle:
                file_handle.write("bars: [\n")

            payload = {
                "requestId": "req-7",
                "command": "apply",
                "args": {"settings": {"watch_config": True}},
                "context": {"dryRun": False},
            }

            with patch.dict(os.environ, {"USERPROFILE": tmp_dir}):
                response = self.run_main(payload)

            backup_dir = os.path.dirname(config_path)
            backups = [
                name for name in os.listdir(backup_dir) if name.startswith("config.yaml.") and name.endswith(".bak")
            ]

            self.assertTrue(response["success"])
            self.assertTrue(response["changed"])
            self.assertEqual(len(backups), 1)

    def test_main_returns_json_error_on_bad_input(self):
        stdin = StringIO("{")
        stdout = StringIO()

        with patch("sys.stdin", stdin), patch("sys.stdout", stdout):
            plugin.main()

        response = json.loads(stdout.getvalue().strip())

        self.assertFalse(response["success"])
        self.assertEqual(response["requestId"], "unknown")
        self.assertIn("Failed to parse request", response["error"])

    def test_main_returns_json_error_on_empty_input(self):
        stdin = StringIO("")
        stdout = StringIO()

        with patch("sys.stdin", stdin), patch("sys.stdout", stdout):
            plugin.main()

        response = json.loads(stdout.getvalue().strip())

        self.assertFalse(response["success"])
        self.assertEqual(response["requestId"], "unknown")
        self.assertIn("empty stdin", response["error"])

    def test_unknown_command(self):
        response = self.run_main({"requestId": "req-6", "command": "explode", "args": {}, "context": {}})

        self.assertFalse(response["success"])
        self.assertIn("error", response)
        self.assertIsNone(response["data"])


if __name__ == "__main__":
    unittest.main()
