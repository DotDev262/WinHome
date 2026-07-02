# scoop plugin

## Description

The `scoop` plugin manages configuration for [Scoop](https://scoop.sh), a command-line installer for
Windows. It writes settings to Scoop's `config.json` file, allowing you to declaratively control
Scoop's behaviour as part of your WinHome setup.

## Prerequisites

- Scoop must be installed (see [scoop.sh](https://scoop.sh))
- Scoop is detected via `scoop.exe`, `scoop.ps1`, `scoop.cmd`, or `scoop`
- **Windows only**

## Configuration file location

| Platform            | Path                                              |
| ------------------- | ------------------------------------------------- |
| Windows (XDG)       | `%XDG_CONFIG_HOME%\scoop\config.json`             |
| Windows (default)   | `%USERPROFILE%\.config\scoop\config.json`          |

The plugin first checks the `XDG_CONFIG_HOME` environment variable, then falls back to
`%USERPROFILE%\.config\scoop`.

## Configuration format

```yaml
plugins:
  scoop:
    settings:
      <key>: <value>
