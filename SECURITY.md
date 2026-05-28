# Security Policy

## Supported Versions

We only support the latest version of WinHome. Please ensure you are using the most recent release before reporting a security vulnerability.

| Version | Supported |
| ------- | --------- |
| Latest | :white_check_mark: |
| < 1.0 | :x: |

---

## Reporting a Vulnerability

We take security seriously. If you discover a security vulnerability within WinHome, please do **not** disclose it publicly.

Please report vulnerabilities using one of the following methods:

1. **Email**: Send a report to [security@winhome.dev](mailto:security@winhome.dev)
2. **GitHub**: Use the [Private Vulnerability Reporting](https://github.com/DotDev262/WinHome/security/advisories/new) feature

### What to Include

To help us triage issues quickly, please include:

- A descriptive title
- A clear explanation of the vulnerability
- Steps to reproduce the issue
- Potential security impact
- Any proof-of-concept scripts or configurations

### Response Process

- Reports will be acknowledged within 48 hours
- Initial assessment will be provided within 5 business days
- Progress updates will be shared during remediation
- Disclosure timelines will be coordinated once a fix is available

Thank you for helping keep WinHome secure. ❤️

---

# RegistryGuard

RegistryGuard is a protection mechanism that prevents unsafe registry modifications, especially when WinHome is executed with elevated SYSTEM privileges.

### Why It Exists

When applications run as SYSTEM, accidental writes to sensitive registry hives like `HKCU` (`HKEY_CURRENT_USER`) can create instability, permission conflicts, or unintended persistence issues.

### What It Does

RegistryGuard helps by:

- Blocking unsafe `HKCU` modifications
- Preventing accidental privilege misuse
- Reducing the risk of system misconfiguration
- Enforcing safer registry interaction patterns

Contributors can review the implementation in [`src/Infrastructure/Helpers/RegistryGuard.cs`](../src/Infrastructure/Helpers/RegistryGuard.cs).

---

# Secret Reference Syntax

WinHome supports secure secret references directly inside `config.yaml`.

## Environment Variables

```yaml
envVars:
  - variable: "API_KEY"
    value: "{{ env:API_KEY }}"
```

Reads the value from a system environment variable.

---

## File-Based Secrets

```yaml
envVars:
  - variable: "TOKEN"
    value: "{{ file:C:\secrets\token.txt }}"
```

Reads the secret value from a local file.

Recommendations:

- Never commit secret files to Git
- Restrict file permissions
- Store secrets outside public directories

---

## Windows Credential Manager

```yaml
envVars:
  - variable: "DB_PASSWORD"
    value: "{{ vault:database-password }}"
```

Reads credentials securely from Windows Credential Manager.

This is the only currently supported vault integration in WinHome.

---

# Security Presets

WinHome provides multiple security presets for different use cases.

## Baseline

Balanced configuration for general users.

### Example

```yaml
security_preset: baseline
```

Recommended for:

- Daily usage
- General desktop systems
- New users

---

## Strict

Aggressive security-focused configuration.

### Example

```yaml
security_preset: strict
```

Recommended for:

- Security-sensitive environments
- Administrative systems
- Shared devices

---

## Privacy

Focused on reducing telemetry and unnecessary data exposure.

### Example

```yaml
security_preset: privacy
```

Recommended for:

- Privacy-conscious users
- Minimal telemetry environments

---

# Safe Registry Tweaking Guidelines

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

# Additional Recommendations

- Keep Windows updated
- Use strong administrator passwords
- Enable system restore points
- Review tweaks before deployment
- Audit scripts before execution

---

# Disclaimer

Advanced registry and system-level modifications may affect system stability. Always verify configurations before applying changes to production environments.
