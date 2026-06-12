# 🔌 WinHome Plugin Directory

WinHome currently ships with 36 built-in plugins under `plugins/`. This page acts as a marketplace-style index for those plugins and a quick reference for how each one is enabled from `config.yaml`.

## Capability Legend

* `config_provider`: The plugin reads a config section and reconciles one or more tool-specific config files.
* `package_manager`: The plugin can also back an `apps:` entry by implementing `check_installed`, `install`, and `uninstall`.

## At A Glance

### Package And Ecosystem

| Name | Brief description | Capabilities | Docs |
| :--- | :--- | :--- | :--- |
| [Core Environment](./core.md) | Core shell and runtime environment configuration engine | `config_provider` | [Details](./core.md) |
| [Scoop](#scoop) | Command-line Installer Provisioning Framework | `package_manager` | [Details](#scoop) |
| [Miniconda](#miniconda) | Python/R Package and Environment Manager | `package_manager` | [Details](#miniconda) |
| [Sdkman](#sdkman) | Software Development Kit Manager for Java Ecosystem | `package_manager` | [Details](#sdkman) |
| [7-Zip](#7-zip) | High-Ratio File Archiver and Compression Tool | - | [Details](#7-zip) |
| [Syncthing](#syncthing) | Continuous File Synchronization Framework | - | [Details](#syncthing) |

### Editors And Knowledge Tools

| Name | Brief description | Capabilities | Docs |
| :--- | :--- | :--- | :--- |
| [Neovim](./neovim.md) | Hyperextensible Vim-based text editor suite profile | `config_provider` | [Details](./neovim.md) |
