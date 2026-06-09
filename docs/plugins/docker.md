# Docker Plugin

## Overview

Normally, if user need to change RAM, CPU, or auto-start settings in Docker, they need to open the
Docker Desktop app and make changes in its UI(settings menu). This plugin saves the user from having
to go into the app's UI. User can write all their Docker settings as code directly into WinHome's
config.yaml file. When user need to run their WinHome, this plugin reads the settings from their
config.yaml and silently adds them to the original Docker settings file
(%APPDATA%\Docker\settings.json) stored in Windows in the background. The biggest advantage of this
is that the entire system of the user and their Docker configurations are managed in one place (in a
clear way).

## Prerequisites

- The user should make sure that the Docker Desktop or Docker Engine must be installed in their
  system.
- The installation will automatically detected by the plugin. It will check for 'docker.exe' or
  'docker' in user's system PATH.
- **Windows only** (this plugin will be able to manage settings inside the '%APPDATA%' directory).

## Configuration Schema

The plugin accepts a top-level YAML object with a single supported field:

| Field      | Type   | Default | Description                                                                                                        |
| :--------- | :----- | :------ | :----------------------------------------------------------------------------------------------------------------- |
| `settings` | object | none    | A dictionary of Docker Engine settings (e.g., `builder`, `experimental`) that will be merged into `settings.json`. |

## Usage Examples

### Example 1 - Configure resources and auto-start

```yaml
extensions:
  docker:
    settings:
      cpus: 4
      memoryMiB: 8192
      autoStart: true
      useWslEngine: true
```

### Example 2 - Configure DNS and networking

```yaml
extensions:
  docker:
    settings:
      useWslEngine: false
      exposeDockerAPIOnTcp2375: false
      dns:
        - '8.8.8.8'
        - '1.1.1.1'
```

### Example 3 - Set registry mirrors and download limits

```yaml
extensions:
  docker:
    settings:
      registryMirrors:
        - 'https://mirror.gcr.io'
      maxConcurrentDownloads: 3
      maxConcurrentUploads: 5
```

## Notes / Caveats

- The plugin deep-merges settings — existing config entries and features not mentioned in user's
  config.yaml are preserved.
- If the settings.json file is corrupted or non-processable, the plugin automatically backs it up
  with a unique suffix and starts fresh.
- It supports dryRun mode — logs what would change in the config path without actually writing to
  disk.
- The plugin deep-merges the provided settings with the existing `settings.json`.
- Existing keys in the native file not mentioned in the WinHome config are safely preserved.

## Verification Steps

To verify that the settings managed by this plugin (such as CPUs, memory, and auto-start) have been
applied correctly:

- The user have to restart Docker Desktop.
- Then they have open the Docker Desktop UI and navigate to the Settings menu to confirm their
  configurations.
