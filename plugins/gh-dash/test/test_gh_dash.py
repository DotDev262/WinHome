import json
import os
import sys
import tempfile
import unittest
from io import StringIO
from unittest.mock import MagicMock, patch

import yaml

sys.path.insert(0, os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "src")))
import plugin


class TestGhDashPlugin(unittest.TestCase):
    def run_main(self, payload: dict) -> dict:
        stdin = StringIO(json.dumps(payload) + "\n")
        stdout = StringIO()
        with patch("sys.stdin", stdin), patch("sys.stdout", stdout):
            plugin.main()
        return json.loads(stdout.getvalue().strip())

    # --- check_installed ---

    def test_check_installed_true_via_which(self):
        with patch("plugin.shutil.which", side_effect=lambda name: "/usr/local/bin/gh-dash" if name == "gh-dash" else None):
            response = self.run_main({"requestId": "req-1", "command": "check_installed", "args": {}, "context": {}})

        self.assertTrue(response["success"])
        self.assertFalse(response["changed"])
        self.assertTrue(response["data"])

    def test_check_installed_true_via_gh_ext(self):
        mock_result = MagicMock()
        mock_result.stdout = "dlvhdr/gh-dash\nsome-other/extension\n"

        with patch("plugin.shutil.which", return_value=None), \
             patch("plugin.subprocess.run", return_value=mock_result):
            response = self.run_main({"requestId": "req-2", "command": "check_installed", "args": {}, "context": {}})

        self.assertTrue(response["success"])
        self.assertTrue(response["data"])

    def test_check_installed_false(self):
        mock_result = MagicMock()
        mock_result.stdout = "some-other/extension\n"

        with patch("plugin.shutil.which", return_value=None), \
             patch("plugin.subprocess.run", return_value=mock_result):
            response = self.run_main({"requestId": "req-3", "command": "check_installed", "args": {}, "context": {}})

        self.assertTrue(response["success"])
        self.assertFalse(response["data"])

    # --- apply ---

    def test_apply_writes_sections(self):
        with tempfile.TemporaryDirectory() as tmp_dir:
            config_path = os.path.join(tmp_dir, "config.yml")

            with patch("plugin.get_config_path", return_value=config_path):
                response = self.run_main({
                    "requestId": "req-4",
                    "command": "apply",
                    "args": {
                        "defaultLimit": 30,
                        "prSections": [{"title": "My PRs", "filters": "is:open author:@me"}],
                    },
                    "context": {"dryRun": False},
                })

            self.assertTrue(response["success"])
            self.assertTrue(response["changed"])

            with open(config_path, "r", encoding="utf-8") as fh:
                content = yaml.safe_load(fh)

            self.assertEqual(content["defaultLimit"], 30)
            self.assertEqual(len(content["prSections"]), 1)
            self.assertEqual(content["prSections"][0]["title"], "My PRs")

    def test_apply_replaces_list_sections_entirely(self):
        with tempfile.TemporaryDirectory() as tmp_dir:
            config_path = os.path.join(tmp_dir, "config.yml")
            initial = {
                "prSections": [
                    {"title": "Section A", "filters": "is:open"},
                    {"title": "Section B", "filters": "is:merged"},
                ]
            }
            with open(config_path, "w", encoding="utf-8") as fh:
                yaml.dump(initial, fh, default_flow_style=False, sort_keys=False)

            with patch("plugin.get_config_path", return_value=config_path):
                response = self.run_main({
                    "requestId": "req-5",
                    "command": "apply",
                    "args": {"prSections": [{"title": "Only Section", "filters": "is:open author:@me"}]},
                    "context": {"dryRun": False},
                })

            self.assertTrue(response["success"])
            self.assertTrue(response["changed"])

            with open(config_path, "r", encoding="utf-8") as fh:
                content = yaml.safe_load(fh)

            self.assertEqual(len(content["prSections"]), 1)
            self.assertEqual(content["prSections"][0]["title"], "Only Section")

    def test_apply_merges_scalar_settings(self):
        with tempfile.TemporaryDirectory() as tmp_dir:
            config_path = os.path.join(tmp_dir, "config.yml")
            with open(config_path, "w", encoding="utf-8") as fh:
                yaml.dump({"defaultLimit": 20}, fh, default_flow_style=False, sort_keys=False)

            with patch("plugin.get_config_path", return_value=config_path):
                response = self.run_main({
                    "requestId": "req-6",
                    "command": "apply",
                    "args": {"refreshInterval": 120},
                    "context": {"dryRun": False},
                })

            self.assertTrue(response["success"])
            self.assertTrue(response["changed"])

            with open(config_path, "r", encoding="utf-8") as fh:
                content = yaml.safe_load(fh)

            self.assertEqual(content["defaultLimit"], 20)
            self.assertEqual(content["refreshInterval"], 120)

    def test_apply_no_changes_returns_changed_false(self):
        with tempfile.TemporaryDirectory() as tmp_dir:
            config_path = os.path.join(tmp_dir, "config.yml")
            initial = {"defaultLimit": 30, "prSections": [{"title": "My PRs", "filters": "is:open author:@me"}]}
            with open(config_path, "w", encoding="utf-8") as fh:
                yaml.dump(initial, fh, default_flow_style=False, sort_keys=False)

            with patch("plugin.get_config_path", return_value=config_path):
                response = self.run_main({
                    "requestId": "req-7",
                    "command": "apply",
                    "args": {"defaultLimit": 30, "prSections": [{"title": "My PRs", "filters": "is:open author:@me"}]},
                    "context": {"dryRun": False},
                })

            self.assertTrue(response["success"])
            self.assertFalse(response["changed"])

    def test_apply_dry_run_does_not_write(self):
        with tempfile.TemporaryDirectory() as tmp_dir:
            config_path = os.path.join(tmp_dir, "subdir", "config.yml")

            with patch("plugin.get_config_path", return_value=config_path):
                response = self.run_main({
                    "requestId": "req-8",
                    "command": "apply",
                    "args": {"defaultLimit": 50},
                    "context": {"dryRun": True},
                })

            self.assertTrue(response["success"])
            self.assertTrue(response["changed"])
            self.assertFalse(os.path.exists(config_path))

    def test_apply_creates_missing_directory(self):
        with tempfile.TemporaryDirectory() as tmp_dir:
            config_path = os.path.join(tmp_dir, "gh-dash", "config.yml")
            self.assertFalse(os.path.isdir(os.path.dirname(config_path)))

            with patch("plugin.get_config_path", return_value=config_path):
                response = self.run_main({
                    "requestId": "req-9",
                    "command": "apply",
                    "args": {"defaultLimit": 30},
                    "context": {"dryRun": False},
                })

            self.assertTrue(response["success"])
            self.assertTrue(response["changed"])
            self.assertTrue(os.path.isdir(os.path.dirname(config_path)))
            self.assertTrue(os.path.exists(config_path))

    def test_apply_preserves_unmentioned_settings(self):
        with tempfile.TemporaryDirectory() as tmp_dir:
            config_path = os.path.join(tmp_dir, "config.yml")
            with open(config_path, "w", encoding="utf-8") as fh:
                yaml.dump({"theme": "dark", "defaultLimit": 20}, fh, default_flow_style=False, sort_keys=False)

            with patch("plugin.get_config_path", return_value=config_path):
                self.run_main({
                    "requestId": "req-10",
                    "command": "apply",
                    "args": {"defaultLimit": 30},
                    "context": {"dryRun": False},
                })

            with open(config_path, "r", encoding="utf-8") as fh:
                content = yaml.safe_load(fh)

            self.assertEqual(content["theme"], "dark")
            self.assertEqual(content["defaultLimit"], 30)

    def test_apply_gh_dash_config_env_var(self):
        with tempfile.TemporaryDirectory() as tmp_dir:
            config_path = os.path.join(tmp_dir, "custom-config.yml")

            with patch.dict(os.environ, {"GH_DASH_CONFIG": config_path}):
                response = self.run_main({
                    "requestId": "req-11",
                    "command": "apply",
                    "args": {"defaultLimit": 25},
                    "context": {"dryRun": False},
                })

            self.assertTrue(response["success"])
            self.assertTrue(response["changed"])
            self.assertTrue(os.path.exists(config_path))

            with open(config_path, "r", encoding="utf-8") as fh:
                content = yaml.safe_load(fh)
            self.assertEqual(content["defaultLimit"], 25)

    def test_apply_uses_settings_wrapper(self):
        with tempfile.TemporaryDirectory() as tmp_dir:
            config_path = os.path.join(tmp_dir, "config.yml")

            with patch("plugin.get_config_path", return_value=config_path):
                response = self.run_main({
                    "requestId": "req-12",
                    "command": "apply",
                    "args": {
                        "settings": {
                            "defaultLimit": 40,
                            "prSections": [{"title": "Wrapped", "filters": "is:open"}],
                        }
                    },
                    "context": {"dryRun": False},
                })

            self.assertTrue(response["success"])
            self.assertTrue(response["changed"])

            with open(config_path, "r", encoding="utf-8") as fh:
                content = yaml.safe_load(fh)

            self.assertEqual(content["defaultLimit"], 40)
            self.assertEqual(content["prSections"][0]["title"], "Wrapped")

    def test_apply_works_without_pyyaml(self):
        with tempfile.TemporaryDirectory() as tmp_dir:
            config_path = os.path.join(tmp_dir, "config.yml")
            with patch("plugin.get_config_path", return_value=config_path), \
                 patch("plugin._HAS_PYYAML", False), \
                 patch("plugin._yaml", None):
                response = self.run_main({
                    "requestId": "req-13",
                    "command": "apply",
                    "args": {"defaultLimit": 30, "prSections": [{"title": "My PRs", "filters": "is:open author:@me"}]},
                    "context": {"dryRun": False},
                })

            self.assertEqual(response["requestId"], "req-13")
            self.assertTrue(response["success"])
            self.assertTrue(response["changed"])

            with open(config_path, "r", encoding="utf-8") as fh:
                content = yaml.safe_load(fh)
            self.assertEqual(content["defaultLimit"], 30)
            self.assertEqual(content["prSections"][0]["title"], "My PRs")

    def test_unknown_command_returns_error(self):
        response = self.run_main({"requestId": "req-14", "command": "explode", "args": {}, "context": {}})
        self.assertEqual(response["requestId"], "req-14")
        self.assertFalse(response["success"])
        self.assertFalse(response["changed"])
        self.assertIn("Unknown command", response["error"])


if __name__ == "__main__":
    unittest.main()
