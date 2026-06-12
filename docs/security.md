# Security Guide

WinHome includes built-in security mechanisms and presets to help users safely optimize and
configure their systems.

---

## RegistryGuard

RegistryGuard is a protection mechanism that prevents unsafe registry modifications, particularly
when WinHome is executed with elevated SYSTEM privileges.

### Why It Exists

When applications run as SYSTEM, accidental writes to sensitive registry hives like `HKCU`
(HKEY_CURRENT_USER) can cause instability, permission conflicts, or unintended persistence issues.

### What It Does

RegistryGuard protects your system by:

- **Blocking** unsafe `HKCU` modifications
- **Preventing** accidental privilege misuse
- **Reducing** the risk of system misconfiguration
- **Enforcing** safer registry interaction patterns

---

## Secret Management Best Practices

WinHome supports multiple approaches for handling sensitive values securely.

### Environment Variables

Environment variables are the recommended approach for managing secrets. Use them for:

| Use Case              | Example           |
| --------------------- | ----------------- |
| API keys              | `$env:API_KEY`    |
| Auth tokens           | `$env:AUTH_TOKEN` |
| CI/CD secrets         | `$env:CI_SECRET`  |
| Temporary credentials | `$env:TEMP_CRED`  |

**Example:**

```powershell
$env:API_KEY = "your_key_here"
```

---

### File-Based Secrets

Useful for:

- Local development
- Offline systems
- Encrypted configuration storage

#### Recommendations

- Never commit secret files to Git
- Restrict file permissions
- Store secrets outside public directories
- Use encrypted storage whenever possible

---

### Windows Credential Manager

```yaml
envVars:
  - variable: 'DB_PASSWORD'
    value: '{{ vault:database-password }}'
```

Reads credentials securely from Windows Credential Manager.

This is the only currently supported vault integration in WinHome.

---

## Security Presets

WinHome provides multiple security presets for different use cases.

### Baseline

Balanced configuration for general users.

#### Recommended For

- Daily usage
- General desktop systems
- New users

#### Includes

- Standard registry protection
- Safer default configurations
- Basic hardening

---

### Strict

Aggressive security-focused configuration.

#### Recommended For

- Security-sensitive environments
- Administrative systems
- Shared devices

#### Includes

- Tighter registry restrictions
- Reduced attack surface
- Enhanced validation

---

### Privacy

Focused on reducing telemetry and unnecessary data exposure.

#### Recommended For

- Privacy-conscious users
- Minimal telemetry environments

#### Includes

- Privacy-oriented defaults
- Reduced tracking behavior
- Additional user-data protections

---

## Safe Registry Tweaking Guidelines

Before modifying the registry:

- Always create backups
- Test changes incrementally
- Avoid unknown registry scripts
- Document modifications carefully
- Use least-privilege principles whenever possible

### Recommended Tools

- Registry Editor (`regedit`)
- PowerShell
- WinHome presets and safety mechanisms

---

## Additional Recommendations

- Keep Windows updated
- Use strong administrator passwords
- Enable system restore points
- Review tweaks before deployment
- Audit scripts before execution

---

## Disclaimer

Advanced registry and system-level modifications may affect system stability. Always verify
configurations before applying changes to production environments.
