# Test PowerShell Plugin

## Overview

The Test PowerShell plugin is a mock/test fixture plugin used to exercise both `config_provider` and `package_manager` capabilities in the WinHome test suite.

## Prerequisites

- PowerShell installed and configured on the Windows machine.

## Configuration Schema

- Key-value pairs specified in settings are processed and logged directly to stderr when applying configuration.
- The plugin handles the following mock commands:
  - `check_installed`: Checks if a mock package (e.g. `"demo-pkg"`) is installed.
  - `install`: Simulates installation of a mock package.
  - `uninstall`: Simulates uninstallation of a mock package.
  - `apply`: Logs all configuration parameters.

## Usage Examples

```yaml
plugins:
  - name: test-powershell
    settings:
      demo_key: "demo_value"
```

## Verification Steps

This plugin is designed to run inside the automated test runner. You can execute the plugin script manually via:
```powershell
powershell -File plugins/test-powershell/plugin.ps1
```

## Notes / Caveats

- This plugin is a test fixture meant as a reference implementation of the JSON IPC protocol and should not be used for user environment setup.
