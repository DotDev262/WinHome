import os
import sys
import tempfile
import unittest
from unittest.mock import patch

# Add src directory to path to import plugin
sys.path.insert(
    0, os.path.abspath(os.path.join(os.path.dirname(__file__), "../src"))
)
import plugin


class TestOpenSSHPlugin(unittest.TestCase):
    def setUp(self):
        self.temp_dir = tempfile.TemporaryDirectory()
        self.config_path = os.path.join(self.temp_dir.name, "config")

    def tearDown(self):
        self.temp_dir.cleanup()

    @patch("plugin.shutil.which")
    def test_check_installed(self, mock_which):
        mock_which.return_value = "/usr/bin/ssh"

        response = plugin.check_installed({}, "req-1")

        self.assertTrue(response["success"])
        self.assertTrue(response["data"]["installed"])
        mock_which.assert_called_once()

    def test_parse_ssh_config(self):
        text = """
# Global settings
UseKeychain yes
AddKeysToAgent yes

Host github.com
    HostName github.com
    User git

# Another host
Host dev-server
    HostName 192.168.1.100
"""
        blocks, _ = plugin.parse_ssh_config(text)
        self.assertEqual(len(blocks), 3)
        self.assertIsNone(blocks[0]["name"])
        self.assertEqual(blocks[1]["name"], "github.com")
        self.assertEqual(blocks[2]["name"], "dev-server")

        # Check global properties
        kv_lines = [line for line in blocks[0]["lines"] if line["type"] == "kv"]
        self.assertEqual(kv_lines[0]["val"], "yes")
        self.assertEqual(kv_lines[0]["key"], "UseKeychain")

    def test_merge_settings_new_global(self):
        blocks, has_tn = plugin.parse_ssh_config("UseKeychain yes\n")
        args = {"global": {"AddKeysToAgent": "yes", "UseKeychain": "no"}}
        changed = plugin.merge_settings(blocks, args)
        self.assertTrue(changed)

        text = plugin.serialize_ssh_config(blocks, has_tn)
        self.assertIn("UseKeychain no", text)
        self.assertIn("AddKeysToAgent yes", text)

    def test_merge_settings_new_host(self):
        blocks, has_tn = plugin.parse_ssh_config(
            "Host github.com\n    User git\n"
        )
        args = {
            "hosts": {
                "github.com": {"HostName": "github.com", "User": "admin"},
                "dev-server": {"HostName": "192.168.1.100", "User": "ubuntu"},
            }
        }
        changed = plugin.merge_settings(blocks, args)
        self.assertTrue(changed)

        text = plugin.serialize_ssh_config(blocks, has_tn)
        self.assertIn("Host github.com", text)
        self.assertIn("User admin", text)
        self.assertIn("HostName github.com", text)
        self.assertIn("Host dev-server", text)
        self.assertIn("HostName 192.168.1.100", text)

    @patch("plugin.get_config_path")
    def test_apply_config_dry_run(self, mock_get_path):
        mock_get_path.return_value = self.config_path

        # Write initial config
        with open(self.config_path, "w", encoding="utf-8") as f:
            f.write("Host dev-server\n    User old_user\n")

        args = {"hosts": {"dev-server": {"User": "new_user"}}}

        # Dry run
        response = plugin.apply_config(args, {"dryRun": True}, "req-2")
        self.assertTrue(response["success"])
        self.assertTrue(response["changed"])

        # Verify file was NOT changed
        with open(self.config_path, "r", encoding="utf-8") as f:
            content = f.read()
        self.assertIn("User old_user", content)
        self.assertNotIn("User new_user", content)

    @patch("plugin.get_config_path")
    def test_apply_config_real_run(self, mock_get_path):
        mock_get_path.return_value = self.config_path

        args = {
            "global": {"StrictHostKeyChecking": "accept-new"},
            "hosts": {"dev-server": {"User": "new_user"}},
        }

        # Real run
        response = plugin.apply_config(args, {"dryRun": False}, "req-3")
        self.assertTrue(response["success"])
        self.assertTrue(response["changed"])

        # Verify file WAS changed
        with open(self.config_path, "r", encoding="utf-8") as f:
            content = f.read()
        self.assertIn("StrictHostKeyChecking accept-new", content)
        self.assertIn("Host dev-server", content)
        self.assertIn("User new_user", content)

    @patch("plugin.get_config_path")
    def test_apply_config_no_changes(self, mock_get_path):
        mock_get_path.return_value = self.config_path

        # Write initial config
        with open(self.config_path, "w", encoding="utf-8") as f:
            f.write("Host dev-server\n    User same_user\n")

        args = {"hosts": {"dev-server": {"User": "same_user"}}}

        # Real run but no actual differences
        response = plugin.apply_config(args, {"dryRun": False}, "req-4")
        self.assertTrue(response["success"])
        self.assertFalse(response["changed"])  # changed should be false

    def test_trailing_newline_roundtrip(self):
        # 1. With trailing newline
        blocks, has_tn = plugin.parse_ssh_config("Host a\n")
        self.assertTrue(has_tn)
        res = plugin.serialize_ssh_config(blocks, has_tn)
        self.assertEqual(res, "Host a\n")

        # 2. Without trailing newline
        blocks, has_tn = plugin.parse_ssh_config("Host b")
        self.assertFalse(has_tn)
        res = plugin.serialize_ssh_config(blocks, has_tn)
        self.assertEqual(res, "Host b")

    @patch("plugin.get_config_path")
    def test_directory_creation(self, mock_get_path):
        # Use a path in a directory that doesn't exist yet
        ssh_dir = os.path.join(self.temp_dir.name, "new_ssh_dir")
        config_path = os.path.join(ssh_dir, "config")
        mock_get_path.return_value = config_path

        args = {"global": {"StrictHostKeyChecking": "no"}}
        plugin.apply_config(args, {}, "req-dir")

        # Verify directory was created
        self.assertTrue(os.path.isdir(ssh_dir))
        self.assertTrue(os.path.isfile(config_path))

    @patch("sys.stdin")
    @patch("sys.stdout")
    @patch("plugin.get_config_path")
    def test_main_integration(self, mock_get_path, mock_stdout, mock_stdin):
        mock_get_path.return_value = self.config_path

        import io
        import json

        req = {
            "requestId": "test-int",
            "command": "apply",
            "args": {"global": {"UseKeychain": "yes"}},
            "context": {"dryRun": False},
        }
        mock_stdin.read.return_value = json.dumps(req)

        fake_stdout = io.StringIO()
        mock_stdout.write.side_effect = fake_stdout.write

        plugin.main()

        # Parse output
        output = fake_stdout.getvalue()
        resp = json.loads(output)

        self.assertTrue(resp["success"])
        self.assertTrue(resp["changed"])

        # Verify it actually wrote the file
        with open(self.config_path, "r", encoding="utf-8") as f:
            self.assertIn("UseKeychain yes", f.read())


if __name__ == "__main__":
    unittest.main()
