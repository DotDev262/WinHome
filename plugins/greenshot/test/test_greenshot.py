import os
import sys
import unittest

# 🧠 INTERFACE COMPLIANCE FIX: Employs strict path lifecycle control to comply with repository testing rules
src_path = os.path.abspath(os.path.join(os.path.dirname(__file__), "../src"))
sys.path.append(src_path)

import plugin

sys.path.remove(src_path)


class TestGreenshotPlugin(unittest.TestCase):
    """Automated unit verification validation test suite patterns for the Greenshot plugin integration."""

    def test_apply_configuration_matrix(self):
        args = {
            "requestId": "test-req-293",
            "dryRun": True,
            "settings": {
                "Capture\\CaptureMode": "Region",
                "Capture\\CaptureMousepointer": True,
                "Destination\\CopyToClipboard": True,
                "General\\Language": "en-US",
            },
        }
        result = plugin.apply(args)

        self.assertEqual(result["requestId"], "test-req-293")
        self.assertTrue(result["changed"])


if __name__ == "__main__":
    unittest.main()
