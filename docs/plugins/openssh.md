# OpenSSH Plugin

## Overview

The `openssh` plugin is used to manage the configuration and settings for the OpenSSH client or
server. It automatically merges the user's defined settings into the appropriate SSH configuration
files, ensuring that their secure connections are set up correctly.

## Prerequisites

- **OpenSSH**: The OpenSSH client or server must be installed on user's system.
- The SSH configuration file (typically `~/.ssh/config` or `/etc/ssh/sshd_config`) should exist and
  be accessible by the user running WinHome.

## Configuration Schema

The plugin accepts a top-level YAML object with the following supported fields:

| Field      | Type   | Default | Description                                                                      |
| :--------- | :----- | :------ | :------------------------------------------------------------------------------- |
| `settings` | object | none    | A dictionary of OpenSSH settings that will be applied to the user's environment. |

## Usage Examples

### Example 1 - Basic configuration

```yaml
extensions:
  openssh:
    settings:
      port: 22
      listenAddress: '0.0.0.0'
```

### Example 2 - Advanced security configuration

```yaml
extensions:
  openssh:
    settings:
      passwordAuthentication: false
      permitRootLogin: false
      maxAuthTries: 3
```

## Notes / Caveats

- The plugin deep-merges settings – existing config entries and features not mentioned in the user's
  config are preserved.
- It supports dryRun mode – logs what would change in the config path without actually writing to
  disk.

## Verification Steps

To verify that the settings managed by this plugin have been applied correctly:

- The user has to restart the OpenSSH service.
- User should try connecting via SSH or check the service status in their terminal to confirm their
  configurations.
