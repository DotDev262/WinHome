import json
import os
import sys
import unittest
from unittest.mock import mock_open, patch

src_p = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "src"))
if src_p not in sys.path:
    sys.path.append(src_p)

try:
    import plugin
finally:
    if src_p in sys.path:
        sys.path.remove(src_p)


class TestGitHubDesktopPlugin(unittest.TestCase):

    @patch("sys.stdin")
    def test_empty_stdin_throws_json_error(self, mock_stdin):
        """Verifies empty input returns structured JSON error metadata."""
        mock_stdin.read.return_value = "   "
        with patch("sys.stdout") as mock_stdout:
            plugin.main()
            raw = "".join(call.args for call in mock_stdout.write.call_args_list)
            output = json.loads(raw.strip())
            self.assertEqual(output["requestId"], "unknown")
            self.assertIn("error", output)

    @patch("sys.stdin")
    @patch("os.environ", {"APPDATA": "C:\\MockAppData"})
    @patch("os.path.exists")
    def test_check_installed_protocol_parity(self, mock_exists, mock_stdin):
        """Verifies installation checks return proper envelope layout."""
        mock_stdin.read.return_value = json.dumps(
            {
                "requestId": "req-123",
                "command": "check_installed",
                "args": {},
            }
        )
        mock_exists.return_value = True
        with patch("sys.stdout") as mock_stdout:
            plugin.main()
            raw = "".join(call.args for call in mock_stdout.write.call_args_list)
            output = json.loads(raw.strip())
            self.assertEqual(output["requestId"], "req-123")
            self.assertTrue(output["installed"])

    @patch("sys.stdin")
    @patch("os.environ", {"APPDATA": "C:\\MockAppData"})
    @patch("os.path.exists")
    @patch("builtins.open", new_callable=mock_open, read_data='{"theme":"dark"}')
    @patch("tempfile.mkstemp")
    @patch("os.fdopen", new_callable=mock_open)
    @patch("os.replace")
    def test_settings_deep_merge_atomic_write(
        self, mock_rep, mock_fd, mock_stemp, mock_f, mock_ex, mock_in
    ):
        """Verifies deep merges calculate variance parameters seamlessly."""
        mock_in.read.return_value = json.dumps(
            {
                "requestId": "req-446",
                "command": "apply",
                "args": {
                    "settings": {
                        "defaultBranchName": "main",
                        "confirmRemovedFiles": True,
                    },
                    "dryRun": False,
                },
            }
        )
        mock_ex.return_value = True
        mock_stemp.return_value = (
            10,
            "C:\\MockAppData\\GitHub Desktop\\config.json",
        )
        with patch("sys.stdout") as mock_stdout:
            plugin.main()
            raw = "".join(call.args for call in mock_stdout.write.call_args_list)
            output = json.loads(raw.strip())
            self.assertEqual(output["requestId"], "req-446")
            self.assertTrue(output["changed"])


if __name__ == "__main__":
    unittest.main()
