import sys
import os
import json
import unittest
from io import StringIO
from unittest.mock import patch

# FIX: Using append() and remove() per project convention!
src_path = os.path.abspath(os.path.join(os.path.dirname(__file__), '../src'))
sys.path.append(src_path)
import plugin
sys.path.remove(src_path)

class TestMinicondaPlugin(unittest.TestCase):
    
    @patch('sys.stdout', new_callable=StringIO)
    @patch('sys.stdin', StringIO('{"requestId": "1", "command": "check_installed"}'))
    @patch('plugin.check_installed', return_value=True)
    def test_check_installed(self, mock_check, mock_stdout):
        plugin.main()
        output = json.loads(mock_stdout.getvalue())
        self.assertEqual(output["requestId"], "1")
        self.assertTrue(output["data"])

    @patch('sys.stdout', new_callable=StringIO)
    @patch('sys.stdin', StringIO(''))
    def test_empty_input(self, mock_stdout):
        plugin.main()
        output = json.loads(mock_stdout.getvalue())
        self.assertIn("error", output)

    @patch('sys.stdout', new_callable=StringIO)
    @patch('sys.stdin', StringIO('{"requestId": "2", "command": "apply", "args": {"dryRun": true, "settings": {"channels": ["conda-forge"]}}}'))
    def test_apply_dry_run(self, mock_stdout):
        plugin.main()
        output = json.loads(mock_stdout.getvalue())
        self.assertEqual(output["requestId"], "2")
        self.assertEqual(output["data"]["status"], "dry-run complete")

if __name__ == '__main__':
    unittest.main()
    