import os
import sys
import json
import unittest
from unittest.mock import patch, MagicMock

# Add src directory to path to import plugin
sys.path.insert(0, os.path.abspath(os.path.join(os.path.dirname(__file__), '../src')))

import plugin

class TestWindowsExplorerPlugin(unittest.TestCase):

    def setUp(self):
        self.maxDiff = None

    def test_check_installed(self):
        result = plugin.check_installed("req-123")
        self.assertEqual(result, {
            "requestId": "req-123",
            "success": True,
            "changed": False,
            "data": True
        })

    @patch('plugin.winreg')
    def test_apply_empty_config(self, mock_winreg):
        args = {"settings": {}}
        context = {}
        result = plugin.apply_config(args, context, "req-456")
        
        self.assertEqual(result, {
            "requestId": "req-456",
            "success": True,
            "changed": False
        })
        mock_winreg.OpenKey.assert_not_called()

    @patch('plugin.read_registry_values')
    @patch('plugin.winreg')
    def test_apply_config_no_changes_needed(self, mock_winreg, mock_read):
        # Current values exactly match desired values
        mock_read.return_value = {
            "HideFileExt": 1,
            "Hidden": 2
        }
        args = {
            "settings": {
                "HideFileExt": True,
                "Hidden": 2
            }
        }
        context = {}
        result = plugin.apply_config(args, context, "req-789")
        
        self.assertEqual(result, {
            "requestId": "req-789",
            "success": True,
            "changed": False
        })
        # SetValueEx should not be called because there's no difference
        mock_winreg.OpenKey.assert_not_called()

    @patch('plugin.read_registry_values')
    @patch('plugin.winreg')
    def test_apply_config_changes_needed(self, mock_winreg, mock_read):
        # Current values are different from desired values
        mock_read.return_value = {
            "HideFileExt": 0,
            "Hidden": 1,
            "ShowSuperHidden": 1
        }
        args = {
            "settings": {
                "HideFileExt": True,       # Desired: 1
                "Hidden": 2,               # Desired: 2
                "ShowSuperHidden": False   # Desired: 0
            }
        }
        context = {}
        
        # Mocking OpenKey context manager
        mock_key = MagicMock()
        mock_winreg.OpenKey.return_value.__enter__.return_value = mock_key
        
        result = plugin.apply_config(args, context, "req-abc")
        
        self.assertEqual(result, {
            "requestId": "req-abc",
            "success": True,
            "changed": True
        })
        
        # Expecting 3 set value calls
        self.assertEqual(mock_winreg.SetValueEx.call_count, 3)
        mock_winreg.SetValueEx.assert_any_call(mock_key, "HideFileExt", 0, mock_winreg.REG_DWORD, 1)
        mock_winreg.SetValueEx.assert_any_call(mock_key, "Hidden", 0, mock_winreg.REG_DWORD, 2)
        mock_winreg.SetValueEx.assert_any_call(mock_key, "ShowSuperHidden", 0, mock_winreg.REG_DWORD, 0)

    @patch('plugin.read_registry_values')
    @patch('plugin.winreg')
    @patch('plugin.log')
    def test_apply_config_dry_run(self, mock_log, mock_winreg, mock_read):
        mock_read.return_value = {
            "HideFileExt": 0
        }
        args = {
            "settings": {
                "HideFileExt": True
            }
        }
        context = {"dryRun": True}
        
        result = plugin.apply_config(args, context, "req-dry")
        
        self.assertEqual(result, {
            "requestId": "req-dry",
            "success": True,
            "changed": True
        })
        
        # OpenKey (and SetValueEx) should NOT be called in a dry run
        mock_winreg.OpenKey.assert_not_called()
        
        # We should log what would have happened
        mock_log.assert_any_call("Dry run: Would update registry key HideFileExt to 1")

    @patch('plugin.read_registry_values')
    @patch('plugin.winreg')
    @patch('plugin.log')
    def test_apply_config_invalid_hidden(self, mock_log, mock_winreg, mock_read):
        mock_read.return_value = {}
        args = {
            "settings": {
                "Hidden": 3 # Invalid value
            }
        }
        context = {}
        
        result = plugin.apply_config(args, context, "req-inv")
        
        # Will be successful but with no changes to 'Hidden'
        self.assertEqual(result, {
            "requestId": "req-inv",
            "success": True,
            "changed": False
        })
        mock_log.assert_any_call("Invalid value for Hidden: 3. Must be 1 or 2.")

    @patch('plugin.read_registry_values')
    @patch('plugin.winreg')
    def test_apply_config_registry_error(self, mock_winreg, mock_read):
        mock_read.return_value = {"HideFileExt": 0}
        args = {"settings": {"HideFileExt": True}}
        context = {}
        
        # Simulate a registry error when writing
        mock_winreg.OpenKey.side_effect = PermissionError("Access is denied")
        
        result = plugin.apply_config(args, context, "req-err")
        
        self.assertEqual(result["requestId"], "req-err")
        self.assertEqual(result["success"], False)
        self.assertEqual(result["changed"], False)
        self.assertTrue("Failed to write to registry" in result["error"])

if __name__ == '__main__':
    unittest.main()
