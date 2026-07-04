# Postman Plugin

The `postman` plugin handles automated setup, workspace integration, and environment mapping configurations for the Postman API development ecosystem. It simplifies API development pipelines by managing underlying environment collections and workspace initialization variables directly through code.

## Prerequisites

- **Supported OS**: Windows 10 / Windows 11 (Both AMD64 and ARM64 system architectures are supported natively)
- **Dependencies**: Postman desktop application installed or active Postman CLI environment configured

## Configuration Format

Configure your plugin parameters securely within your root `config.yaml` using these supported setting attributes:

```yaml
plugins:
  postman:
    enabled: true              # Activates or deactivates the Postman integration framework
    api_key: "your-api-key"    # Optional: Secure token for syncing workspace environment variables
    workspace_id: "id"        # Optional: Direct ID mapping target to isolate collection groups
    auto_update: true          # Optional: Automatically updates collections upon platform boots
```

## Real-World Usage Examples

### 1. Minimal Configuration
Basic activation setting targeting global local workspace structures:
```yaml
plugins:
  postman:
    enabled: true
```

### 2. Fully Cloud-Synchronized API Framework
Advanced production configuration mapping remote environment credentials and auto-update tracking pipelines:
```yaml
plugins:
  postman:
    enabled: true
    api_key: "pm_apikey_example_string_987654321"
    workspace_id: "wsp-prod-alpha-99"
    auto_update: true
```

### 3. Staging and Development Testing Environment Isolation
Configures localized workspace groups for sandboxed validation tasks without automatic remote fetches:
```yaml
plugins:
  postman:
    enabled: true
    workspace_id: "wsp-staging-beta-44"
    auto_update: false
```

## Verification Steps

To confirm that the Postman plugin has been initialized and is functioning correctly across your target environment configurations, execute these evaluation checks:

1. Run the system diagnostics command tool string to audit active plugin configurations:
   ```bash
   winhome doctor
   ```
2. Verify that the output print streams explicitly log a successful initialization handshake for the postman module component:
   ```text
   [SUCCESS] Plugin 'postman' initialized successfully.
   [INFO] Workspace ID 'wsp-prod-alpha-99' validated and mapped.
   ```
3. Inspect your local telemetry files or environment configurations to ensure postman collection definitions are successfully mounted.
