# SDKMAN Plugin

## Overview

The SDKMAN plugin manages configuration settings in the `config` file for the SDKMAN toolchain manager.

## Prerequisites

- SDKMAN installed (directory `%USERPROFILE%\.sdkman` exists)

## Configuration Schema

Settings accept key-value pairs under the `settings` key (mapped to standard properties file format `key=value`):

- Boolean values are automatically cast to `"true"` or `"false"` strings.

## Usage Examples

```yaml
plugins:
  - name: sdkman
    settings:
      sdkman_auto_answer: true
      sdkman_selfupdate_feature: false
```

## Verification Steps

To verify settings, examine the SDKMAN configuration file:
```bash
cat ~/.sdkman/etc/config
```

## Notes / Caveats

- Settings are modified persistently in `%USERPROFILE%\.sdkman\etc\config`.
- Mapped options are merged safely while preserving existing keys in the configuration.
