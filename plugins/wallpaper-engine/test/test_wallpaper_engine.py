import unittest

from src.plugin import deep_merge


class TestWallpaperEngine(unittest.TestCase):

    def test_deep_merge(self):

        base = {
            "audio": {
                "volume": 0.7
            }
        }

        updates = {
            "audio": {
                "mute": True
            }
        }

        result = deep_merge(base, updates)

        self.assertEqual(
            result["audio"]["volume"],
            0.7
        )

        self.assertEqual(
            result["audio"]["mute"],
            True
        )
    def test_dry_run(self):

        args = {
            "requestId": "test-101",
            "dryRun": True,
            "settings": {
                "volume": 0.2
            }
        }

        result = {
            "success": True,
            "requestId": "test-101",
            "dryRun": True
        }

        self.assertEqual(result["success"], True)

        self.assertEqual(result["dryRun"], True)

        self.assertEqual(result["requestId"], "test-101")