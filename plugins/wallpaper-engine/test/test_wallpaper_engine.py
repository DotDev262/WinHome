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

    def run_plugin_subprocess(self, payload):
        proc = subprocess.Popen(
            [sys.executable, self.plugin_script],
            stdin=subprocess.PIPE,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            text=True
        )
        stdout, _ = proc.communicate(input=json.dumps(payload))
        return json.loads(stdout.strip())

    def test_protocol_check_installed(self):
        payload = {"requestId": "req-001", "command": "check_installed"}
        response = self.run_plugin_subprocess(payload)
        self.assertEqual(response["requestId"], "req-001")
        self.assertEqual(response["status"], "success")
        self.assertFalse(response["installed"])

    def test_protocol_apply_changes(self):
        os.makedirs(self.config_dir, exist_ok=True)
        with open(self.config_file, "w") as f:
            f.write(json.dumps({"volume": 0.2}))
            
        payload = {
            "requestId": "req-002",
            "command": "apply",
            "args": {
                "settings": {"volume": 0.9, "fps": 60},
                "dryRun": False
            }
        }
        
        response = self.run_plugin_subprocess(payload)
        self.assertEqual(response["requestId"], "req-002")
        self.assertTrue(response["changed"])
        
        with open(self.config_file, "r") as f:
            data = json.load(f)
        self.assertEqual(data["volume"], 0.9)
        self.assertEqual(data["fps"], 60)

if __name__ == "__main__":
    unittest.main()
