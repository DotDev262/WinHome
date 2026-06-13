# ☕ Sdkman Plugin

## 📋 Overview

The Sdkman plugin manages parallel versions of multiple Software Development Kits for the Java ecosystem seamlessly.

## 🛠️ Prerequisites

- Zip and Curl utilities active inside terminal shell

## 🗄️ Configuration Schema

| Key | Type | Description | Required |
| :--- | :--- | :--- | :--- |
| `candidates` | `Map` | SDK runtime environments and specific versions to pin | Yes |

## 💻 Usage Examples

```yaml
plugins:
  sdkman:
    candidates:
      java: 17.0.7-tem
      gradle: 8.1.1
```

## 🔍 Verification Steps

```bash
sdk list java
```

## ⚠️ Notes & Caveats

- Modifies baseline shell environmental profile variables (`.bashrc` / `.zshrc`).
