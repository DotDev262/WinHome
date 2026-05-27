import unittest
import sys
import os
from pathlib import Path

# Fix import path
current_dir = os.path.dirname(os.path.abspath(__file__))
src_dir = os.path.abspath(os.path.join(current_dir, '..', 'src'))
sys.path.insert(0, src_dir)

from plugin import read_ini, check_installed, apply

class TestEverythingPlugin(unittest.TestCase):

    def test_check_installed(self):
        result = check_installed()
        self.assertIn('installed', result)
        self.assertIsInstance(result['installed'], bool)

    def test_apply_dry_run(self):
        request = {
            'command': 'apply',
            'args': {
                'dry_run': True,
                'args': {
                    'general': {
                        'run_as_admin': True,
                        'show_tray_icon': True
                    }
                }
            }
        }
        result = apply(request)
        self.assertTrue(result['success'])
        self.assertTrue(result['changed'])

    def test_read_ini_nonexistent(self):
        result = read_ini()
        self.assertIsInstance(result, dict)

if __name__ == '__main__':
    unittest.main(verbosity=2)
