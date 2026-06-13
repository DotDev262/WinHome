# Environment Variables

Configures Windows environment variables for your system or user profile.

**YAML Key:** `env`

**Properties:**

- `name` : Name of the environment variable.
- `value` : Value to set.
- `scope` : `user` or `system`.

---

## Basic Usage

```yaml
env:
  - name: 'MY_VAR'
    value: 'my_value'
    scope: 'user'
```

---

## Real-World Examples

### Example 1 — Developer Setup

```yaml
env:
  - name: 'JAVA_HOME'
    value: "C:\\Program Files\\Java\\jdk-17"
    scope: 'system'
  - name: 'NODE_ENV'
    value: 'development'
    scope: 'user'
```

### Example 2 — Python Setup

```yaml
env:
  - name: 'PYTHONPATH'
    value: "C:\\Python311"
    scope: 'system'
  - name: 'PIP_DEFAULT_TIMEOUT'
    value: '100'
    scope: 'user'
```

### Example 3 — Work Setup

```yaml
env:
  - name: 'COMPANY_API_KEY'
    value: 'your_api_key'
    scope: 'user'
  - name: 'PROXY_URL'
    value: 'http://proxy.company.com'
    scope: 'system'
```

### Example 4 — Minimal Setup

```yaml
env:
  - name: 'MY_PROJECT'
    value: "C:\\Projects"
    scope: 'user'
```

---

## Troubleshooting

**Issue: Variable not found after setting**

- Restart terminal after setting variables
- Log out and log back in for system variables
- Run `echo %MY_VAR%` to verify

**Issue: Wrong scope**

- Use `system` for all users
- Use `user` for current user only
- Run WinHome as Administrator for system scope

**Issue: Value not applying**

- Check for typos in variable name
- Make sure WinHome ran successfully
- Verify in System Properties > Environment Variables
