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

---

## 🍦 Scoop

### 📋 Overview
The Scoop plugin enables WinHome to declaratively install, update, and manage developer command-line utilities and tools seamlessly on Windows environments without triggering UAC popups.

### 🗄️ Configuration Schema
```yaml
plugins:
  scoop:
    buckets:
      - extras
    packages:
      - git
      - neovim
      - curl
```

---

## 🐍 Miniconda

### 📋 Overview
The Miniconda plugin automates the provisioning of light-weight conda package, dependency, and environment management systems across active terminal workflows.

### 🗄️ Configuration Schema
```yaml
plugins:
  miniconda:
    channels:
      - conda-forge
    packages:
      - python=3.10
      - numpy
```

---

## ☕ Sdkman

### 📋 Overview
The Sdkman plugin manages parallel versions of multiple Software Development Kits for the Java ecosystem, including Java JDKs, Groovy, Gradle, and Maven.

### 🗄️ Configuration Schema
```yaml
plugins:
  sdkman:
    candidates:
      java: 17.0.7-tem
      gradle: 8.1.1
```

---

## 📦 7-Zip

### 📋 Overview
The 7-Zip plugin provides high-ratio archive decompression and packing routines natively accessible across system automation hooks.

### 🗄️ Configuration Schema
```yaml
plugins:
  7-zip:
    install_path: C:\Program Files\7-Zip
```

---

## 🔄 Syncthing

### 📋 Overview
The Syncthing plugin deploys a decentralized, peer-to-peer decentralized file synchronization engine across multiple node networks.

### 🗄️ Configuration Schema
```yaml
plugins:
  syncthing:
    gui_port: 8384
    folders:
      - path: ~/Development
```
