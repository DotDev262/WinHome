import importlib.util
import json
import subprocess
import unittest
from io import StringIO
from pathlib import Path
from unittest.mock import patch

plugin_path = Path(__file__).parent.parent / "src" / "plugin.py"
spec = importlib.util.spec_from_file_location("go_plugin", plugin_path)
plugin = importlib.util.module_from_spec(spec)
spec.loader.exec_module(plugin)


def completed(stdout="", stderr="", returncode=0):
    return subprocess.CompletedProcess(
        args=["go"],
        returncode=returncode,
        stdout=stdout,
        stderr=stderr,
    )


class TestGoPluginInstalled(unittest.TestCase):
    def test_check_installed_true(self):
        with patch.object(plugin.shutil, "which", side_effect=lambda name: "/usr/bin/go" if name == "go" else None):
            result = plugin.check_installed({}, "req-installed")

        self.assertTrue(result["success"])
        self.assertFalse(result["changed"])
        self.assertTrue(result["data"])
        self.assertEqual(result["requestId"], "req-installed")

    def test_check_installed_false(self):
        with patch.object(plugin.shutil, "which", return_value=None):
            result = plugin.check_installed({}, "req-missing")

        self.assertTrue(result["success"])
        self.assertFalse(result["data"])


class TestGoEnvParsing(unittest.TestCase):
    def test_read_go_env_strips_trailing_newlines_only(self):
        with patch.object(plugin.subprocess, "run", return_value=completed(stdout="/tmp/go\n")) as run:
            value = plugin.read_go_env("/usr/bin/go", "GOPATH")

        self.assertEqual(value, "/tmp/go")
        run.assert_called_once_with(
            ["/usr/bin/go", "env", "GOPATH"],
            capture_output=True,
            check=False,
            text=True,
        )

    def test_read_go_env_raises_clean_stderr(self):
        with patch.object(plugin.subprocess, "run", return_value=completed(stderr="bad key\n", returncode=1)):
            with self.assertRaisesRegex(RuntimeError, "bad key"):
                plugin.read_go_env("/usr/bin/go", "GOPATH")


class TestGoApply(unittest.TestCase):
    def test_dry_run_reports_expected_changes_without_writing(self):
        calls = []

        def fake_run(cmd, capture_output, check, text):
            calls.append(cmd)
            if cmd == ["/usr/bin/go", "env", "GOPATH"]:
                return completed(stdout="/old/path\n")
            if cmd == ["/usr/bin/go", "env", "GO111MODULE"]:
                return completed(stdout="auto\n")
            raise AssertionError(f"unexpected command: {cmd}")

        with patch.object(plugin, "go_executable", return_value="/usr/bin/go"):
            with patch.object(plugin.subprocess, "run", side_effect=fake_run):
                result = plugin.apply_config(
                    {"settings": {"GOPATH": "/new/path", "GO111MODULE": "on"}},
                    {"dryRun": True},
                    "req-dry",
                )

        self.assertTrue(result["success"])
        self.assertTrue(result["changed"])
        self.assertEqual(result["data"]["planned"]["GOPATH"]["current"], "/old/path")
        self.assertEqual(result["data"]["planned"]["GOPATH"]["desired"], "/new/path")
        self.assertNotIn(["/usr/bin/go", "env", "-w", "GOPATH=/new/path"], calls)

    def test_apply_sets_only_changed_values(self):
        calls = []

        def fake_run(cmd, capture_output, check, text):
            calls.append(cmd)
            if cmd == ["/usr/bin/go", "env", "GOPATH"]:
                return completed(stdout="/old/path\n")
            if cmd == ["/usr/bin/go", "env", "GOOS"]:
                return completed(stdout="linux\n")
            if cmd == ["/usr/bin/go", "env", "-w", "GOPATH=/new/path"]:
                return completed()
            raise AssertionError(f"unexpected command: {cmd}")

        with patch.object(plugin, "go_executable", return_value="/usr/bin/go"):
            with patch.object(plugin.subprocess, "run", side_effect=fake_run):
                result = plugin.apply_config(
                    {"settings": {"GOPATH": "/new/path", "GOOS": "linux"}},
                    {},
                    "req-apply",
                )

        self.assertTrue(result["success"])
        self.assertTrue(result["changed"])
        self.assertIn(["/usr/bin/go", "env", "-w", "GOPATH=/new/path"], calls)
        self.assertNotIn(["/usr/bin/go", "env", "-w", "GOOS=linux"], calls)

    def test_noop_when_values_already_match(self):
        with patch.object(plugin, "go_executable", return_value="/usr/bin/go"):
            with patch.object(
                plugin.subprocess, "run", return_value=completed(stdout="https://proxy.golang.org,direct\n")
            ):
                result = plugin.apply_config(
                    {"settings": {"GOPROXY": "https://proxy.golang.org,direct"}},
                    {},
                    "req-noop",
                )

        self.assertTrue(result["success"])
        self.assertFalse(result["changed"])
        self.assertEqual(result["data"]["planned"], {})

    def test_missing_go_is_graceful(self):
        with patch.object(plugin, "go_executable", return_value=None):
            result = plugin.apply_config({"settings": {"GOPATH": "/tmp/go"}}, {}, "req-no-go")

        self.assertFalse(result["success"])
        self.assertFalse(result["changed"])
        self.assertIn("Go executable not found", result["error"])

    def test_rejects_unsupported_setting(self):
        with patch.object(plugin, "go_executable", return_value="/usr/bin/go"):
            result = plugin.apply_config({"settings": {"NOT_GO_ENV": "x"}}, {}, "req-bad")

        self.assertFalse(result["success"])
        self.assertIn("Unsupported Go environment setting", result["error"])


class TestGoProtocol(unittest.TestCase):
    def run_main(self, payload):
        with patch.object(plugin.sys, "stdin", StringIO(payload)):
            with patch.object(plugin.sys, "stdout", StringIO()) as stdout:
                plugin.main()
                return json.loads(stdout.getvalue())

    def test_handle_uses_settings_from_args(self):
        response = {"requestId": "req", "success": True, "changed": False}
        with patch.object(plugin, "apply_config", return_value=response) as apply:
            result = plugin.handle(
                {
                    "requestId": "req",
                    "command": "apply",
                    "args": {"settings": {"GOPATH": "/workspace/go"}},
                    "context": {},
                }
            )

        self.assertTrue(result["success"])
        apply.assert_called_once_with({"settings": {"GOPATH": "/workspace/go"}}, {}, "req")

    def test_main_returns_json_error_for_invalid_json(self):
        result = self.run_main("{not json")

        self.assertFalse(result["success"])
        self.assertEqual(result["requestId"], "unknown")
        self.assertIn("Expecting property name", result["error"])

    def test_main_returns_json_error_for_empty_input(self):
        result = self.run_main("")

        self.assertFalse(result["success"])
        self.assertEqual(result["requestId"], "unknown")
        self.assertEqual(result["error"], "No input provided")

    def test_unknown_command(self):
        result = plugin.handle({"requestId": "req-unknown", "command": "wat", "args": {}, "context": {}})

        self.assertFalse(result["success"])
        self.assertEqual(result["error"], "Unknown command: wat")


if __name__ == "__main__":
    unittest.main()
