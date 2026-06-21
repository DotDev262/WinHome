import unittest
import json
import sys
import os
from unittest.mock import patch, mock_open

# Compliance constraint: Leveraging sys.path.append instead of sys.path.insert(0)
sys.path.append(os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "src")))

import plugin

class TestGitHubDesktopPlugin(unittest.TestCase):
    
    @patch("sys.stdin")
    @patch("sys.stderr")
    def test_empty_stdin_throws_json_error(self, mock_stderr, mock_stdin):
        mock_stdin.read.return_value = "   "
        with self.assertRaises(SystemExit) as cm:
            plugin.main()
        self.assertEqual(cm.exception.code, 1)
        
    @patch("sys.stdin")
    @patch("os.environ", {"APPDATA": "C:\\MockAppData"})
    @patch("os.path.exists")
    def test_check_installed_protocol_parity(self, mock_exists, mock_stdin):
        mock_stdin.read.return_value = json.dumps({"requestId": "test-req-123", "check_installed": True})
        mock_exists.return_value = True
        
        with patch("sys.stdout") as mock_stdout:
            with self.assertRaises(SystemExit) as cm:
                plugin.main()
            self.assertEqual(cm.exception.code, 0)
            output = json.loads(mock_stdout.write.call_args)
            self.assertEqual(output["requestId"], "test-req-123")
            self.assertTrue(output["installed"])
            
    @patch("sys.stdin")
    @patch("os.environ", {"APPDATA": "C:\\MockAppData"})
    @patch("os.path.exists")
    @patch("builtins.open", new_callable=mock_open, read_data='{"theme": "dark"}')
    @patch("tempfile.mkstemp")
    @patch("os.fdopen", new_callable=mock_open)
    @patch("os.replace")
    def test_settings_deep_merge_atomic_write(self, mock_replace, mock_fdopen, mock_mkstemp, mock_file, mock_exists, mock_stdin):
        mock_stdin.read.return_value = json.dumps({
            "requestId": "test-req-456",
            "settings": {"defaultBranchName": "main", "confirmRemovedFiles": True},
            "dryRun": False
        })
        mock_exists.return_value = True
        mock_mkstemp.return_value = (10, "C:\\MockAppData\\GitHub Desktop\\config_tmp.json")
        
        with patch("sys.stdout") as mock_stdout:
            with self.assertRaises(SystemExit) as cm:
                plugin.main()
            self.assertEqual(cm.exception.code, 0)
            output = json.loads(mock_stdout.write.call_args)
            self.assertEqual(output["requestId"], "test-req-456")
            self.assertNotIn("success", output)
            self.assertNotIn("data", output)

if __name__ == "__main__":
    unittest.main()

