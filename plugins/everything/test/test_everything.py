import unittest
import sys
import os

# Fix import
current_dir = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.abspath(os.path.join(current_dir, '..', 'src')))

from plugin import check_installed, apply

class TestEverythingPlugin(unittest.TestCase):

    def test_check_installed(self):
        result = check_installed()
        self.assertIn('installed', result)

    def test_apply_dry_run(self):
        request = {
            "command": "apply",
            "args": {
                "dry_run": True,
                "args": {
                    "general": {
                        "run_as_admin": True,
                        "show_in_taskbar": True
                    }
                }
            }
        }
        result = apply(request)
        self.assertTrue(result["success"])

if __name__ == '__main__':
    unittest.main(verbosity=2)
