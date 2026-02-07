# v1.2.0 - The Fortified Release 🛡️

This release marks a significant milestone in **WinHome's** maturity, introducing a powerful new Plugin Architecture alongside enterprise-grade security and reliability enhancements.

### ✨ Highlights
*   **Security First**: Major hardening against common attack vectors and administrative pitfalls, including IPC sandboxing and Context Guards.
*   **Extensibility**: A new process-based plugin system that supports Python, Node.js (Bun), and native binaries.
*   **Reliability**: "Write-Through" state management ensures your progress is never lost, even if the process crashes.

### 🚀 New Features
*   **VS Code Integration**: A built-in plugin to manage VS Code profiles, extensions, and settings declaratively.
*   **Plugin Architecture**: Extend WinHome with your own scripts in `~/.winhome/plugins`. Plugins run in a sandboxed environment with strict resource limits.
*   **Write-Through State**: System state is now persisted to disk immediately after every successful action, eliminating "zombie state" inconsistencies.
*   **Lazy Plugin Discovery**: The CLI is cleaner and faster, only initializing plugins that are explicitly used in your configuration.

### 🛡️ Security Fixes
*   **IPC Memory Limits**: Implemented strict 10MB memory limits and 30-second timeouts for all plugins to prevent Denial of Service (DoS) attacks.
*   **Zombie State Fix**: Fixed a critical issue where terminating the process (Ctrl+C) would leave the state file out of sync with the actual system.
*   **Admin Context Guard**: Added `RegistryGuard` to prevent accidental modification of the Administrator's `HKCU` hive when running as `SYSTEM`.

### 📦 Artifacts

| Filename | SHA256 Checksum |
| :--- | :--- |
| `WinHome.exe` | `(Run Get-FileHash to generate)` |

---
