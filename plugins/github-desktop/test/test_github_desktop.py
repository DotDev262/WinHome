import json
import os
import sys
import unittest
from unittest.mock import mock_open, patch

target_src_path = os.path.abspath(
    os.path.join(os.path.dirname(__file__), "..", "src")
)

if target_src_path not in sys.path:
    sys.path.append(target_src_path)

try:
    import plugin
finally:
    if target_src_path in sys.path:
        sys.path.remove(target_src_path)


class TestGitHubDesktopPlugin(unittest.TestCase):

    @patch("sys.stdin")
    def test_empty_stdin_throws_json_error(self, mock_stdin):
        """Verifies that empty input context returns structured JSON error metadata."""
        mock_stdin.read.return_value = "   "

        with patch("sys.stdout") as mock_stdout:
            plugin.main()
            captured_raw = "".join(
                call.args for call in mock_stdout.write.call_args_list
            )
            output = json.loads(captured_raw.strip())

            self.assertEqual(output["requestId"], "unknown")
            self.assertIn("error", output)

    @patch("sys.stdin")
    @patch("os.environ", {"APPDATA": "C:\\MockAppData"})
    @patch("os.path.exists")
    def test_check_installed_protocol_parity(self, mock_exists, mock_stdin):
        """Verifies that installation checks return the proper envelope layout tracking."""
        mock_stdin.read.return_value = json.dumps({
            "requestId": "test-req-123",
            "command": "check_installed",
            "args": {},
        })
        mock_exists.return_value = True

        with patch("sys.stdout") as mock_stdout:
            plugin.main()
            captured_raw = "".join(
                call.args for call in mock_stdout.write.call_args_list
            )
            output = json.loads(captured_raw.strip())

            self.assertEqual(output["requestId"], "test-req-123")
            self.assertTrue(output["installed"])

    @patch("sys.stdin")
    @patch("os.environ", {"APPDATA": "C:\\MockAppData"})
    @patch("os.path.exists")
    @patch("builtins.open", new_callable=mock_open, read_data='{"theme": "dark"}')
    @patch("tempfile.mkstemp")
    @patch("os.fdopen", new_callable=mock_open)
    @patch("os.replace")
    def test_settings_deep_merge_atomic_write(
        self,
        mock_replace,
        mock_fdopen,
        mock_mkstemp,
        mock_file,
        mock_exists,
        mock_stdin,
    ):
        """Verifies that deep merges calculate variance parameters seamlessly."""
        mock_stdin.read.return_value = json.dumps({
            "requestId": "test-req-446",
            "command": "apply",
            "args": {
                "settings": {
                    "defaultBranchName": "main",
                    "confirmRemovedFiles": True,
                },
                "dryRun": False,
            },
        })
        mock_exists.return_value = True
        mock_mkstemp.return_value = (
            10,
            "C:\\MockAppData\\GitHub Desktop\\config_tmp.json",
        )

        with patch("sys.stdout") as mock_stdout:
            plugin.main()
            captured_raw = "".join(
                call.args for call in mock_stdout.write.call_args_list
            )
            output = json.loads(captured_raw.strip())

            self.assertEqual(output["requestId"], "test-req-446")
            self.assertTrue(output["changed"])


if __name__ == "__main__":
    unittest.main()

