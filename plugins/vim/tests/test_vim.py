import sys
import json
import os
import unittest
from unittest.mock import patch, MagicMock, mock_open
import io

# Import the main logic
sys.path.append(os.path.join(os.path.dirname(__file__), "..", "src"))
import main

class TestVimPlugin(unittest.TestCase):

    @patch("os.path.isdir")
    def test_check_installed_returns_true_if_dir_exists(self, mock_isdir):
        mock_isdir.return_value = True
        args = {"packageId": "tpope/vim-fugitive"}
        result = main.check_installed(args)
        self.assertTrue(result["success"])
        self.assertTrue(result["data"])

    @patch("main.NVIM_CONFIG_DIR", "/tmp/nvim")
    @patch("main.INIT_LUA_PATH", "/tmp/nvim/init.lua")
    @patch("os.path.exists")
    @patch("os.makedirs")
    @patch("builtins.open", new_callable=mock_open)
    def test_apply_config_generates_correct_lua(self, mock_file, mock_makedirs, mock_exists):
        mock_exists.return_value = False
        config = {
            "settings": {
                "number": True,
                "theme": "gruvbox",
                "shiftwidth": 4
            }
        }
        context = {"dryRun": False}
        
        result = main.apply_config(config, context)
        
        self.assertTrue(result["success"])
        self.assertTrue(result["changed"])
        
        # Verify content
        written_content = "".join(call.args[0] for call in mock_file().write.call_args_list)
        self.assertIn("vim.opt.number = true", written_content)
        self.assertIn("vim.cmd('colorscheme gruvbox')", written_content)
        self.assertIn("vim.opt.shiftwidth = 4", written_content)

    @patch("sys.stdin", io.StringIO(json.dumps({
        "command": "check_installed",
        "args": {"packageId": "test/plugin"},
        "requestId": "123"
    })))
    @patch("sys.stdout", new_callable=io.StringIO)
    @patch("main.check_installed")
    def test_main_routing(self, mock_check, mock_stdout):
        mock_check.return_value = {"success": True, "data": True}
        
        main.main()
        
        response = json.loads(mock_stdout.getvalue())
        self.assertEqual(response["requestId"], "123")
        self.assertTrue(response["success"])

if __name__ == "__main__":
    unittest.main()
