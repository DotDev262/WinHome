import unittest
import json
import os
import sys
from io import StringIO
from unittest.mock import patch

# Add src to sys.path so we can import plugin
sys.path.insert(0, os.path.abspath(os.path.join(os.path.dirname(__file__), '../src')))
import plugin

class TestHelixPlugin(unittest.TestCase):
    def setUp(self):
        self.mock_helix_dir = os.path.join(os.path.dirname(__file__), 'mock_helix_dir')
        if not os.path.exists(self.mock_helix_dir):
            os.makedirs(self.mock_helix_dir)

    def tearDown(self):
        import shutil
        if os.path.exists(self.mock_helix_dir):
            shutil.rmtree(self.mock_helix_dir)

    @patch('plugin.get_helix_dir')
    def test_apply_config_standard(self, mock_get_helix_dir):
        mock_get_helix_dir.return_value = self.mock_helix_dir
        
        args = {
            "config": {
                "theme": "dark",
                "editor": {
                    "line-number": "relative"
                }
            },
            "languages": {
                "language": [
                    {"name": "python", "auto-format": True}
                ]
            }
        }
        
        context = {"dryRun": False}
        result = plugin.apply_config(args, context, "req1")
        
        self.assertTrue(result["success"])
        self.assertTrue(result["changed"])
        
        # Verify files were written
        self.assertTrue(os.path.exists(os.path.join(self.mock_helix_dir, "config.toml")))
        self.assertTrue(os.path.exists(os.path.join(self.mock_helix_dir, "languages.toml")))
        
        config_toml = open(os.path.join(self.mock_helix_dir, "config.toml")).read()
        self.assertIn('theme = "dark"', config_toml)
        self.assertIn('[editor]', config_toml)
        self.assertIn('line-number = "relative"', config_toml)
        
        languages_toml = open(os.path.join(self.mock_helix_dir, "languages.toml")).read()
        self.assertIn('[[language]]', languages_toml)
        self.assertIn('name = "python"', languages_toml)
        self.assertIn('auto-format = true', languages_toml)

    @patch('plugin.get_helix_dir')
    def test_apply_config_dry_run(self, mock_get_helix_dir):
        mock_get_helix_dir.return_value = self.mock_helix_dir
        
        args = {"config": {"theme": "light"}}
        context = {"dryRun": True}
        
        result = plugin.apply_config(args, context, "req2")
        self.assertTrue(result["success"])
        self.assertTrue(result["changed"])
        
        # Verify file NOT written
        self.assertFalse(os.path.exists(os.path.join(self.mock_helix_dir, "config.toml")))

    @patch('plugin.get_helix_dir')
    def test_apply_config_idempotent(self, mock_get_helix_dir):
        mock_get_helix_dir.return_value = self.mock_helix_dir
        
        args = {
            "config": {"theme": "dark"},
            "languages": {"language": [{"name": "python", "auto-format": True}]}
        }
        context = {"dryRun": False}
        
        # First apply
        result1 = plugin.apply_config(args, context, "req3")
        self.assertTrue(result1["success"])
        self.assertTrue(result1["changed"])
        
        # Second apply (should be idempotent)
        result2 = plugin.apply_config(args, context, "req4")
        self.assertTrue(result2["success"])
        self.assertFalse(result2["changed"])

    @patch('plugin.get_helix_dir')
    def test_unwrapped_args(self, mock_get_helix_dir):
        mock_get_helix_dir.return_value = self.mock_helix_dir
        
        # Helix users might pass flat structure
        args = {
            "theme": "dracula",
            "editor": {"cursor-shape": {"insert": "bar"}},
            "language": [{"name": "rust", "auto-format": True}]
        }
        context = {"dryRun": False}
        
        result = plugin.apply_config(args, context, "req5")
        self.assertTrue(result["success"])
        
        config_toml = open(os.path.join(self.mock_helix_dir, "config.toml")).read()
        self.assertIn('theme = "dracula"', config_toml)
        self.assertIn('[editor.cursor-shape]', config_toml)
        self.assertIn('insert = "bar"', config_toml)
        
        languages_toml = open(os.path.join(self.mock_helix_dir, "languages.toml")).read()
        self.assertIn('[[language]]', languages_toml)
        self.assertIn('name = "rust"', languages_toml)

    @patch('sys.stdin', StringIO('{"command": "unknown", "requestId": "123"}'))
    @patch('sys.stdout', new_callable=StringIO)
    def test_unknown_command(self, mock_stdout):
        plugin.main()
        output = mock_stdout.getvalue()
        resp = json.loads(output)
        self.assertFalse(resp["success"])
        self.assertIn("Unknown command", resp["error"])

if __name__ == '__main__':
    unittest.main()
