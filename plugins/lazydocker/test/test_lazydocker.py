import importlib.util
import json
import os
import shutil
import tempfile
import unittest
from io import StringIO
from pathlib import Path
from unittest.mock import patch

try:
    import yaml
except ImportError:
    yaml = None

plugin_path = Path(__file__).parent.parent / "src" / "plugin.py"
spec = importlib.util.spec_from_file_location("plugin", plugin_path)
plugin = importlib.util.module_from_spec(spec)
spec.loader.exec_module(plugin)


class TestLazyDockerPlugin(unittest.TestCase):
    def setUp(self):
        self.temp_dir = tempfile.mkdtemp()
        self.config_path = os.path.join(self.temp_dir, "lazydocker", "config.yml")

    def tearDown(self):
        shutil.rmtree(self.temp_dir)

    @unittest.skipIf(yaml is None, "PyYAML not installed")
    def test_deep_merge(self):
        dict1 = {"gui": {"theme": {"activeBorderColor": ["blue"]}}, "logs": {"timestamps": False}}
        dict2 = {"gui": {"theme": {"activeBorderColor": ["green", "bold"]}, "scrollHeight": 3}}

        merged, changed = plugin.deep_merge(dict1, dict2)

        self.assertTrue(changed)
        self.assertEqual(merged["gui"]["theme"]["activeBorderColor"], ["green", "bold"])
        self.assertEqual(merged["gui"]["scrollHeight"], 3)
        self.assertEqual(merged["logs"]["timestamps"], False)

    @unittest.skipIf(yaml is None, "PyYAML not installed")
    def test_deep_merge_no_change(self):
        dict1 = {"gui": {"theme": {"activeBorderColor": ["blue"]}}}
        dict2 = {"gui": {"theme": {"activeBorderColor": ["blue"]}}}

        merged, changed = plugin.deep_merge(dict1, dict2)

        self.assertFalse(changed)

    @patch.object(plugin.shutil, "which")
    def test_check_installed_true(self, mock_which):
        mock_which.return_value = "/usr/bin/lazydocker"
        response = plugin.check_installed({}, "req-1")
        self.assertTrue(response["success"])
        self.assertTrue(response["data"])
        self.assertEqual(response["requestId"], "req-1")

    @patch.object(plugin.shutil, "which")
    def test_check_installed_false(self, mock_which):
        mock_which.return_value = None
        response = plugin.check_installed({}, "req-2")
        self.assertTrue(response["success"])
        self.assertFalse(response["data"])

    @patch.object(plugin, "get_config_path")
    @unittest.skipIf(yaml is None, "PyYAML not installed")
    def test_apply_config_creates_new(self, mock_get_path):
        mock_get_path.return_value = self.config_path

        args = {"settings": {"gui": {"language": "auto", "returnImmediately": False}}}
        response = plugin.apply_config(args, {}, "req-3")

        self.assertTrue(response["success"])
        self.assertTrue(response["changed"])

        # Verify file contents
        with open(self.config_path, "r", encoding="utf-8") as f:
            data = yaml.safe_load(f)

        self.assertEqual(data["gui"]["language"], "auto")
        self.assertEqual(data["gui"]["returnImmediately"], False)

    @patch.object(plugin, "get_config_path")
    @unittest.skipIf(yaml is None, "PyYAML not installed")
    def test_apply_config_idempotency(self, mock_get_path):
        mock_get_path.return_value = self.config_path

        args = {"settings": {"logs": {"since": "60m"}}}
        plugin.apply_config(args, {}, "req-4a")

        response = plugin.apply_config(args, {}, "req-4b")

        self.assertTrue(response["success"])
        self.assertFalse(response["changed"])

    @patch.object(plugin, "get_config_path")
    @unittest.skipIf(yaml is None, "PyYAML not installed")
    def test_apply_config_dry_run(self, mock_get_path):
        mock_get_path.return_value = self.config_path

        args = {"settings": {"stats": {"graphs": True}}}
        context = {"dryRun": True}
        response = plugin.apply_config(args, context, "req-5")

        self.assertTrue(response["success"])
        self.assertTrue(response["changed"])
        self.assertFalse(os.path.exists(self.config_path))

    @patch("sys.stdin", new_callable=StringIO)
    @patch("sys.stdout", new_callable=StringIO)
    def test_invalid_json(self, mock_stdout, mock_stdin):
        mock_stdin.write("INVALID { JSON")
        mock_stdin.seek(0)

        plugin.main()

        output = mock_stdout.getvalue()
        response = json.loads(output)

        self.assertFalse(response["success"])
        self.assertIn("Expecting value", response["error"])
        self.assertEqual(response["requestId"], "unknown")


if __name__ == "__main__":
    unittest.main()
