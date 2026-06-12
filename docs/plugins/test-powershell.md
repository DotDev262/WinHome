# Test PowerShell Plugin

## Overview

The Test PowerShell plugin is a sample plugin that demonstrates both `config_provider` and
`package_manager` capabilities. It is primarily used by the test suite to validate the plugin
protocol for PowerShell-based plugins.

It can:

- Simulate package installation status checks
- Simulate package install and uninstall operations
- Apply arbitrary configuration key-value pairs

## Prerequisites

- Windows PowerShell 5.1 or later
- Windows environment

## Configuration Schema

| Field    | Type   | Default | Description                                    |
| -------- | ------ | ------- | ---------------------------------------------- |
| settings | object | none    | Arbitrary key-value settings for demonstration |

### Settings Example

```yaml
settings:
  exampleKey: 'exampleValue'
  featureFlag: true
```

## Usage Examples

### Basic Apply

```yaml
extensions:
  test-powershell:
    settings:
      exampleKey: 'exampleValue'
```

### Package Management (for demo purposes)

```yaml
apps:
  - id: 'demo-pkg'
    manager: 'test-powershell'
```

## Verification Steps

1. Apply the configuration.
2. Run the plugin test suite to confirm all protocol commands work.
3. Verify the plugin responds correctly to `check_installed`, `install`, `uninstall`, and `apply`
   commands.

## Notes / Caveats

- This is a reference implementation, not intended for real package management.
- The simulated `check_installed` reports `demo-pkg` as installed and everything else as not
  installed.
- The plugin uses the process-based JSON-over-Stdin/Stdout IPC protocol.
- It can be used as a template for creating new PowerShell-based plugins.
