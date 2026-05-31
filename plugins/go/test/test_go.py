import importlib.util
import json
import unittest
from io import StringIO
from pathlib import Path
from unittest.mock import patch

plugin_path = Path(__file__).parent.parent / "src" / "plugin.py"
spec = importlib.util.spec_from_file_location("plugin", plugin_path)
plugin = importlib.util.module_from_spec(spec)
spec.loader.exec_module(plugin)


class TestGoPlugin(unittest.TestCase):
    @patch.object(plugin, "find_go_executable")
    def test_check_installed_true(self, mock_find):
        mock_find.return_value = "/usr/local/bin/go"
        response = plugin.check_installed({}, "req-1")
        self.assertTrue(response["success"])
        self.assertTrue(response["data"])
        self.assertEqual(response["requestId"], "req-1")

    @patch.object(plugin, "find_go_executable")
    def test_check_installed_false(self, mock_find):
        mock_find.return_value = None
        response = plugin.check_installed({}, "req-2")
        self.assertTrue(response["success"])
        self.assertFalse(response["data"])

    def test_normalize_go_value_strips_quotes(self):
        self.assertEqual(plugin.normalize_go_value('"C:\\go"'), "C:\\go")
        self.assertEqual(plugin.normalize_go_value("on"), "on")

    def test_run_go_env_parses_multiple_keys(self):
        with patch.object(plugin.subprocess, "run") as mock_run:
            mock_run.return_value.returncode = 0
            mock_run.return_value.stdout = "C:\\Users\\dev\\go\nwindows\n"
            mock_run.return_value.stderr = ""

            result = plugin.run_go_env("/go/bin/go", ["GOPATH", "GOOS"])

        self.assertEqual(result["GOPATH"], "C:\\Users\\dev\\go")
        self.assertEqual(result["GOOS"], "windows")

    @patch.object(plugin, "find_go_executable")
    @patch.object(plugin, "read_go_env_value")
    def test_apply_config_dry_run(self, mock_read, mock_find):
        mock_find.return_value = "/go/bin/go"
        mock_read.return_value = "off"

        response = plugin.apply_config(
            {"settings": {"GO111MODULE": "on"}},
            {"dryRun": True},
            "req-dry",
        )

        self.assertTrue(response["success"])
        self.assertTrue(response["changed"])
        mock_read.assert_called_once()

    @patch.object(plugin, "find_go_executable")
    def test_apply_config_go_not_installed(self, mock_find):
        mock_find.return_value = None
        response = plugin.apply_config(
            {"settings": {"GOPROXY": "https://proxy.golang.org,direct"}},
            {},
            "req-missing",
        )
        self.assertFalse(response["success"])
        self.assertIn("not installed", response["error"])

    @patch.object(plugin, "find_go_executable")
    @patch.object(plugin, "read_go_env_value")
    @patch.object(plugin, "write_go_env_value")
    def test_apply_config_noop_when_already_set(
        self, mock_write, mock_read, mock_find
    ):
        mock_find.return_value = "/go/bin/go"
        mock_read.return_value = "on"

        response = plugin.apply_config(
            {"settings": {"GO111MODULE": "on"}},
            {},
            "req-noop",
        )

        self.assertTrue(response["success"])
        self.assertFalse(response["changed"])
        mock_write.assert_not_called()

    @patch.object(plugin, "find_go_executable")
    @patch.object(plugin, "read_go_env_value")
    @patch.object(plugin, "write_go_env_value")
    def test_apply_config_writes_changes(
        self, mock_write, mock_read, mock_find
    ):
        mock_find.return_value = "/go/bin/go"
        mock_read.return_value = "off"

        response = plugin.apply_config(
            {"settings": {"GO111MODULE": "on", "GOPROXY": "direct"}},
            {},
            "req-write",
        )

        self.assertTrue(response["success"])
        self.assertTrue(response["changed"])
        self.assertEqual(mock_write.call_count, 2)

    @patch.object(plugin, "find_go_executable")
    @patch.object(plugin, "read_go_env_value")
    @patch.object(plugin, "write_go_env_value")
    def test_apply_config_skips_unknown_keys(
        self, mock_write, mock_read, mock_find
    ):
        mock_find.return_value = "/go/bin/go"

        response = plugin.apply_config(
            {"settings": {"NOT_A_GO_ENV": "x"}},
            {},
            "req-skip",
        )

        self.assertTrue(response["success"])
        self.assertFalse(response["changed"])
        mock_write.assert_not_called()
        mock_read.assert_not_called()

    @patch("sys.stdin", new_callable=StringIO)
    @patch("sys.stdout", new_callable=StringIO)
    def test_invalid_json(self, mock_stdout, mock_stdin):
        mock_stdin.write("not json")
        mock_stdin.seek(0)

        plugin.main()

        response = json.loads(mock_stdout.getvalue())
        self.assertFalse(response["success"])
        self.assertIn("Failed to parse request", response["error"])


if __name__ == "__main__":
    unittest.main()
