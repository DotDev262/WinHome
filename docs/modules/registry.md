# Registry

Configures Windows Registry settings for your environment.

**YAML Key:** `registry`

**Properties:**

- `key` : Registry key path.
- `name` : Name of the registry value.
- `value` : Value to set.
- `type` : Registry value type.

---

## Basic Usage

```yaml
registry:
  - key: "HKCU\\Software\\MyApp"
    name: 'Setting1'
    value: 'enabled'
    type: 'String'
```

---

## Real-World Examples

### Example 1 — Enable Dark Mode

```yaml
registry:
  - key: "HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize"
    name: 'AppsUseLightTheme'
    value: 0
    type: 'DWord'
```

### Example 2 — Disable Startup Sound

```yaml
registry:
  - key: "HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System"
    name: 'DisableStartupSound'
    value: 1
    type: 'DWord'
```

### Example 3 — Show File Extensions

```yaml
registry:
  - key: "HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced"
    name: 'HideFileExt'
    value: 0
    type: 'DWord'
```

### Example 4 — Multiple Settings

```yaml
registry:
  - key: "HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize"
    name: 'AppsUseLightTheme'
    value: 0
    type: 'DWord'
  - key: "HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced"
    name: 'HideFileExt'
    value: 0
    type: 'DWord'
```

---

## Troubleshooting

**Issue: Registry key not found**

- Check if key path is correct
- Use Registry Editor to verify path
- Run `regedit` to open Registry Editor

**Issue: Access denied**

- Run WinHome as Administrator
- Some keys require system privileges
- Check key permissions in Registry Editor

**Issue: Wrong value type**

- Use DWord for numbers
- Use String for text values
- Check existing value type in Registry Editor
