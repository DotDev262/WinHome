# 🍦 Scoop Plugin

## 📋 Overview

The Scoop plugin enables WinHome to declaratively install, update, and manage developer command-line utilities and tools seamlessly on Windows environments without administrative privilege requirements.

## 🛠️ Prerequisites

- PowerShell 5.1+ execution capabilities active
- Valid user-level directory environment write permissions

## 🗄️ Configuration Schema

| Key | Type | Description | Required |
| :--- | :--- | :--- | :--- |
| `packages` | `List` | List of target package string names to provision | Yes |
| `buckets` | `List` | Optional external bucket repositories to add | No |

## 💻 Usage Examples

```yaml
plugins:
  scoop:
    buckets:
      - extras
    packages:
      - git
      - neovim
```

## 🔍 Verification Steps

```powershell
scoop list
```

## ⚠️ Notes & Caveats

- Scoop installs applications strictly inside the current user home scope directory (`~/scoop`).
