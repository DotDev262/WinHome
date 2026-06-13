# 📦 7-Zip Plugin

## 📋 Overview

The 7-Zip plugin provides high-ratio archive decompression and packing routines natively accessible across system automation hooks.

## 🛠️ Prerequisites

- Valid destination platform storage blocks active

## 🗄️ Configuration Schema

| Key | Type | Description | Required |
| :--- | :--- | :--- | :--- |
| `install_path` | `String` | Target platform deployment folder location | Yes |

## 💻 Usage Examples

```yaml
plugins:
  7-zip:
    install_path: C:\Program Files\7-Zip
```

## 🔍 Verification Steps

```powershell
7z --help
```

## ⚠️ Notes & Caveats

- Ensure execution path mappings are registered inside your user environments.
