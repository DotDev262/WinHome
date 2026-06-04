import sys
import os
import unittest

# Ensure src module directory path is injected cleanly for localized testing
sys.path.insert(0, os.path.abspath(os.path.join(os.path.dirname(__file__), '../src')))

from plugin import check_installed, deep_merge

class TestRustupPlugin(unittest.TestCase):
    def test_check_installed_returns_bool(self):
        result = check_installed()
        self.assertIn(result, [True, False])

    def test_deep_merge_logic(self):
        src = {"settings": {"profile": "minimal"}}
        dest = {"settings": {"default_toolchain": "stable"}, "custom_key": 123}
        merged = deep_merge(src, dest)
        self.assertEqual(merged["settings"]["profile"], "minimal")
        self.assertEqual(merged["settings"]["default_toolchain"], "stable")
        self.assertEqual(merged["custom_key"], 123)

if __name__ == '__main__':
    unittest.main()
