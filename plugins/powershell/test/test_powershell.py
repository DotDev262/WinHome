import os
import sys
import unittest
from unittest.mock import patch

sys.path.insert(
    0, os.path.abspath(os.path.join(os.path.dirname(__file__), "../src"))
)
import plugin


class TestPowershellPlugin(unittest.TestCase):
    @patch("shutil.which")
    def test_check_installed_true(self, mock_which):
        mock_which.return_value = "/path/to/pwsh.exe"
        res = plugin.check_installed({}, "req-1")
        self.assertTrue(res["success"])
        self.assertTrue(res["data"]["installed"])

    def test_generate_script(self):
        settings = {
            "aliases": {"g": "git", "ll": "ls -Force"},
            "modules": {"zoxide": {"init": {"cmd": "z", "hook": "pwd"}}},
            "prompt": {"type": "oh-my-posh", "theme": "catppuccin"},
            "psreadline": {"edit_mode": "Emacs"},
            "functions": {"touch": "New-Item -ItemType File $args[0]"},
        }
        script = plugin.generate_script(settings)
        self.assertIn("# --- WinHome managed start ---", script)
        self.assertIn("Set-Alias -Name 'g' -Value 'git' -Force", script)
        self.assertIn("function ll { ls -Force @args }", script)
        self.assertIn(
            "Invoke-Expression (& zoxide init powershell --cmd z --hook pwd | Out-String)",
            script,
        )
        self.assertIn(
            "oh-my-posh init powershell --config 'catppuccin'", script
        )
        self.assertIn("Set-PSReadLineOption -EditMode 'Emacs'", script)
        self.assertIn("function touch {", script)
        self.assertIn("# --- WinHome managed end ---", script)


if __name__ == "__main__":
    unittest.main()
