#!/usr/bin/env python3
import os
import sys
import json
import unittest
import tempfile
import shutil
import subprocess

class TestWallpaperEnginePluginContract(unittest.TestCase):
    def setUp(self):
        self.test_dir = tempfile.mkdtemp()
        self.config_dir = os.path.join(self.test_dir, "Steam", "steamapps", "common", "wallpaper_engine", "config")
        self.config_file = os.path.join(self.config_dir, "config.json")
        self.plugin_script = os.path.abspath(os.path.join(os.path.dirname(__file__), "../src/plugin.py"))
        
        self.orig_p86 = os.environ.get("ProgramFiles(x86)")
        os.environ["ProgramFiles(x86)"] = self.test_dir

    def tearDown(self):
        shutil.rmtree(self.test_dir)
        if self.orig_p86:
            os.environ["ProgramFiles(x86)"] = self.orig_p86

    def run_plugin_subprocess(self, payload_str):
        proc = subprocess.Popen(
            [sys.executable, self.plugin_script],
            stdin=subprocess.PIPE,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            text=True
        )
        stdout, _ = proc.communicate(input=payload_str)
        return json.loads(stdout.strip())

    def test_empty_stdin_returns_error(self):
        response = self.run_plugin_subprocess("")
        self.assertEqual(response["requestId"], "unknown")
        self.assertEqual(response["error"], "No input received")

    def test_invalid_json_returns_error(self):
        response = self.run_plugin_subprocess("{invalid-json")
        self.assertEqual(response["requestId"], "unknown")
        self.assertTrue(response["error"].startswith("Invalid JSON:"))

    def test_unknown_command(self):
        payload = {"requestId": "req-999", "command": "unknown_action"}
        response = self.run_plugin_subprocess(json.dumps(payload))
        self.assertEqual(response["requestId"], "req-999")
        self.assertEqual(response["error"], "Unknown command: unknown_action")

    def test_protocol_check_installed(self):
        payload = {"requestId": "req-001", "command": "check_installed"}
        response = self.run_plugin_subprocess(json.dumps(payload))
        self.assertEqual(response["requestId"], "req-001")
        self.assertFalse(response["installed"])
        self.assertNotIn("status", response)

    def test_apply_config_dry_run(self):
        payload = {
            "requestId": "req-002",
            "command": "apply",
            "args": {
                "settings": {"volume": 0.8},
                "dryRun": True
            }
        }
        response = self.run_plugin_subprocess(json.dumps(payload))
        self.assertEqual(response["requestId"], "req-002")
        self.assertTrue(response["dryRun"])
        self.assertTrue(response["changed"])
        self.assertFalse(os.path.exists(self.config_file))

    def test_protocol_apply_changes(self):
        os.makedirs(self.config_dir, exist_ok=True)
        with open(self.config_file, "w") as f:
            f.write(json.dumps({"volume": 0.2}))
            
        payload = {
            "requestId": "req-003",
            "command": "apply",
            "args": {
                "settings": {"volume": 0.9, "fps": 60},
                "dryRun": False
            }
        }
        
        response = self.run_plugin_subprocess(json.dumps(payload))
        self.assertEqual(response["requestId"], "req-003")
        self.assertTrue(response["changed"])
        self.assertIn("path", response)
        
        with open(self.config_file, "r") as f:
            data = json.load(f)
        self.assertEqual(data["volume"], 0.9)
        self.assertEqual(data["fps"], 60)

    def test_idempotent_apply(self):
        os.makedirs(self.config_dir, exist_ok=True)
        payload = {
            "requestId": "req-004",
            "command": "apply",
            "args": {"settings": {"fps": 60}}
        }
        
        res1 = self.run_plugin_subprocess(json.dumps(payload))
        self.assertTrue(res1["changed"])
        
        res2 = self.run_plugin_subprocess(json.dumps(payload))
        self.assertFalse(res2["changed"])

if __name__ == "__main__":
    unittest.main()
