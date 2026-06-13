# 🐍 Miniconda Plugin

## 📋 Overview

The Miniconda plugin automates the provisioning of light-weight conda package, dependency, and environment management systems across active terminal workflows.

## 🛠️ Prerequisites

- Python baseline environment dependencies configured

## 🗄️ Configuration Schema

| Key | Type | Description | Required |
| :--- | :--- | :--- | :--- |
| `packages` | `List` | Core python or science package modules to provision | Yes |
| `channels` | `List` | External environment search channels | No |

## 💻 Usage Examples

```yaml
plugins:
  miniconda:
    channels:
      - conda-forge
    packages:
      - python=3.10
      - numpy
```

## 🔍 Verification Steps

```bash
conda env list
```

## ⚠️ Notes & Caveats

- Relies on standard network connections to download target wheel distributions.
