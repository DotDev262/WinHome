# 🔄 Syncthing Plugin

## 📋 Overview

The Syncthing plugin deploys a decentralized, peer-to-peer decentralized file synchronization engine across node networks.

## 🛠️ Prerequisites

- Authorized localized local storage write permissions active

## 🗄️ Configuration Schema

| Key | Type | Description | Required |
| :--- | :--- | :--- | :--- |
| `gui_port` | `Integer` | Administrative interface dashboard web entry port | Yes |
| `folders` | `List` | Local path strings to monitor and broadcast | No |

## 💻 Usage Examples

```yaml
plugins:
  syncthing:
    gui_port: 8384
    folders:
      - path: ~/Development
```

## 🔍 Verification Steps

```bash
syncthing --version
```

## ⚠️ Notes & Caveats

- Requires network port exposure mapping clearance definitions.
