# Lazygit Plugin

## Overview

Lazygit is a command-line tool that makes using Git easy. Normally, if someone wants to change the
Lazygit theme, set a custom editor, or turn on a setting (like auto-fetch), they have to find the
%APPDATA%\lazygit\config.yml file on their system and make changes to it.This plugin makes life
easier for the user. With it, the user doesn't need to dig up a separate Lazygit file. The user can
write all their Lazygit settings directly into WinHome's central config.yaml file (as code). When
the user runs WinHome, this plugin reads those settings and safely merges them with the original
Lazygit config file, silently in the background, so that old settings are not corrupted.The biggest
advantage: the user's entire system and Lazygit settings are no longer scattered, but are managed
from one place (WinHome), in an absolutely clean way.

## Prerequisites

- The user should ensure that the Lazygit must be installed in their system.
- The plugin detects the installation automatically by checking for `lazygit.exe` or `lazygit` in
  your system's PATH.

## Configuration Schema

The plugin accepts a top-level YAML object with a single supported field:

| Field      | Type   | Default | Description                                                                                     |
| :--------- | :----- | :------ | :---------------------------------------------------------------------------------------------- |
| `settings` | object | none    | Recursively merged into `config.yml`. Any nested object shape supported by Lazygit is accepted. |

## Usage Examples

### Example 1 - Configure UI theme and auto-fetch

```yaml
extensions:
  lazygit:
    settings:
      git:
        autoFetch: true
      gui:
        theme:
          activeBorderColor:
            - green
            - bold
```

### Example 2 - Set a custom code editor and scroll height

```yaml
extensions:
  lazygit:
    settings:
      os:
        editCommand: 'code'
        editCommandTemplate: '{{editor}} {{filename}}'
      gui:
        scrollHeight: 2
```

## Notes / Caveats

- The plugin deep-merges settings — existing config entries in Lazygit's config.yml that are not
  mentioned in user's config.yaml are safely preserved.
- It supports dryRun mode — if it is enabled, it logs what would change in the config path without
  actually writing the updates to disk.
- It safely reads and parses the existing YAML. If the file doesn't exist, it creates a fresh
  configuration.
- Objects are merged recursively.
- Non-object values replace the existing value.
- New keys are added.
- If the current config file is missing, the plugin starts from an empty object.

## Verification Steps

After applying, user can verify if Lazygit is running properly with their applied settings. For
this, user have to open its terminal:

```bash
lazygit
```
