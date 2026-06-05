import os
import sys
import unittest

src_path = os.path.abspath(os.path.join(os.path.dirname(__file__), '../src'))

sys.path.append(src_path)
import plugin
sys.path.remove(src_path)

class TestRustupPlugin(unittest.TestCase):
    def test_check_installed_returns_bool(self):
        result = plugin.check_installed()
        self.assertIn(result, [True, False])

    def test_deep_merge_logic(self):
        src = {"settings": {"profile": "minimal"}}
        dest = {"settings": {"default_toolchain": "stable"}, "custom_key": 123}
        merged = plugin.deep_merge(src, dest)
        self.assertEqual(merged["settings"]["profile"], "minimal")
        self.assertEqual(merged["settings"]["default_toolchain"], "stable")
        self.assertEqual(merged["custom_key"], 123)

if __name__ == '__main__':
    unittest.main()
