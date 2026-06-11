import sys
import os
import json
import unittest
import tempfile
from pathlib import Path
from io import StringIO
from unittest.mock import patch

src_path = os.path.abspath(os.path.join(os.path.dirname(__file__), '../src'))
sys.path.append(src_path)
import plugin
sys.path.remove(src_path)

class TestMinicondaPlugin(unittest.TestCase):
    
    @patch('sys.stdout', new_callable=StringIO)
    @patch('sys.stdin', StringIO('{"requestId": "1", "command": "check_installed"}'))
    @patch('plugin.check_installed', return_value=True)
    def test_check_installed_true(self, mock_check, mock_stdout):
        plugin.main()
        output = json.loads(mock_stdout.getvalue())
        self.assertTrue(output["data"])

    @patch('sys.stdout', new_callable=StringIO)
    @patch('sys.stdin', StringIO('{"requestId": "1", "command": "check_installed"}'))
    @patch('plugin.check_installed', return_value=False)
    def test_check_installed_false(self, mock_check, mock_stdout):
        plugin.main()
        output = json.loads(mock_stdout.getvalue())
        self.assertFalse(output["data"])

    @patch('sys.stdout', new_callable=StringIO)
    @patch('sys.stdin', StringIO(''))
    def test_empty_input(self, mock_stdout):
        plugin.main()
        output = json.loads(mock_stdout.getvalue())
        self.assertIn("error", output)

    @patch('sys.stdout', new_callable=StringIO)
    @patch('sys.stdin', StringIO('invalid json data'))
    def test_invalid_json(self, mock_stdout):
        plugin.main()
        output = json.loads(mock_stdout.getvalue())
        self.assertIn("error", output)

    @patch('sys.stdout', new_callable=StringIO)
    @patch('sys.stdin', StringIO('{"requestId": "2", "command": "unknown_cmd"}'))
    def test_unknown_command(self, mock_stdout):
        plugin.main()
        output = json.loads(mock_stdout.getvalue())
        self.assertIn("error", output)
        
    @patch('sys.stdout', new_callable=StringIO)
    @patch('sys.stdin', StringIO('{"requestId": null, "command": "check_installed"}'))
    @patch('plugin.check_installed', return_value=True)
    def test_null_request_id(self, mock_check, mock_stdout):
        plugin.main()
        output = json.loads(mock_stdout.getvalue())
        self.assertEqual(output["requestId"], "unknown")

    @patch('sys.stdout', new_callable=StringIO)
    @patch('sys.stdin', StringIO('{"requestId": "2", "command": "apply", "args": {"dryRun": true, "settings": {"channels": ["conda-forge"]}}}'))
    def test_apply_dry_run(self, mock_stdout):
        plugin.main()
        output = json.loads(mock_stdout.getvalue())
        self.assertEqual(output["data"]["status"], "dry-run complete")

    @patch('sys.stdout', new_callable=StringIO)
    def test_actual_apply_and_idempotent(self, mock_stdout):
        with tempfile.TemporaryDirectory() as tmpdir:
            fake_condarc = os.path.join(tmpdir, ".condarc")
            
            # Test actual apply (creates file)
            input_data1 = json.dumps({"requestId": "3", "command": "apply", "args": {"settings": {"channels": ["conda-forge"]}}})
            with patch('sys.stdin', StringIO(input_data1)):
                with patch('plugin.Path.home', return_value=Path(tmpdir)):
                    plugin.main()
            
            output1 = mock_stdout.getvalue().strip().split('\n')[-1]
            res1 = json.loads(output1)
            self.assertTrue(res1["changed"])
            self.assertEqual(res1["data"]["status"], "success")
            self.assertTrue(os.path.exists(fake_condarc))

            # Test idempotent apply (runs again, but no changes)
            input_data2 = json.dumps({"requestId": "4", "command": "apply", "args": {"settings": {"channels": ["conda-forge"]}}})
            with patch('sys.stdin', StringIO(input_data2)):
                with patch('plugin.Path.home', return_value=Path(tmpdir)):
                    plugin.main()
                    
            output2 = mock_stdout.getvalue().strip().split('\n')[-1]
            res2 = json.loads(output2)
            self.assertFalse(res2["changed"])
            self.assertEqual(res2["data"]["status"], "success")

if __name__ == '__main__':
    unittest.main()
