# Windows Terminal Plugin

## Overview

The Windows Terminal plugin manages Windows Terminal configuration settings.

It can:

- Update Windows Terminal settings.json
- Merge new settings with existing configuration
- Create a settings file if one does not exist
- Configure profiles, appearance, startup behavior, and terminal preferences

## Prerequisites

- Windows Terminal installed
- Windows environment with `LOCALAPPDATA` or `USERPROFILE` configured
- Permission to modify Windows Terminal settings files

## Configuration Schema

| Field                              | Type                      | Default | Description                         |
| ---------------------------------- | ------------------------- | ------- | ----------------------------------- |
| any valid Windows Terminal setting | object/string/number/bool | none    | Written directly into settings.json |

### Basic Settings Example

```yaml
defaultProfile: '{profile-guid}'
theme: 'dark'
```

### Appearance Example

```yaml
theme: 'dark'
alwaysShowTabs: true
showTabsInTitlebar: false
```

## Usage Examples

### Configure Theme

```yaml
theme: 'dark'
```

### Startup Preferences

```yaml
launchMode: 'maximized'
alwaysShowTabs: true
```

### Profile Defaults

```yaml
profiles:
  defaults:
    font:
      face: 'Cascadia Code'
      size: 12
```

## Verification Steps

1. Apply the configuration.
2. Open Windows Terminal.
3. Open Settings.
4. Verify the configured values appear in the settings UI.
5. Confirm visual or behavioral changes are applied.

## Notes / Caveats

- Existing settings are merged rather than replaced.
- Unknown settings are written without validation.
- Configuration is stored in Windows Terminal's settings.json file.
- The plugin automatically locates the active Windows Terminal configuration.
- If no configuration exists, a new settings file is created.
