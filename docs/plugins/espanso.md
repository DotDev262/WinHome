# Espanso Plugin

## Overview

Espanso is a 'text expander' tool. Its main purpose is to save the user's time and prevent them from
typing the same thing again and again. For example, if the user sets a shortcut (trigger) :email,
then whenever :email is typed anywhere on the computer, Espanso will automatically convert it to the
user's full email address. Normally, to create or edit these shortcuts, the user has to find the
%APPDATA%\espanso\match\base.yml file in the system and make manual changes to it.This plugin makes
the entire process extremely simple: The user doesn't have to dig through a separate Espanso
configuration file. The user can write all their text shortcuts (matches) and variables
(global_vars) directly into WinHome's central config.yml file. When WinHome runs, this plugin
silently merges those settings in the background and safely merges them with the original Espanso
base.yml file. It merges smartly: If there are any old shortcuts already in the native file that the
user hasn't written to the WinHome config, the plugin doesn't delete them; they remain intact.

## Prerequisites

- The user must ensure that the Espanso must be installed in their system. -The plugin detects the
  installation automatically by checking if the `%APPDATA%\espanso` directory exists.

## Configuration Schema

The plugin accepts a top-level YAML object containing Espanso configurations:

| Field      | Type   | Default | Description                                                                            |
| :--------- | :----- | :------ | :------------------------------------------------------------------------------------- |
| `settings` | object | none    | Contains the `matches` and `global_vars` lists to be merged into Espanso's `base.yml`. |

## Usage Examples

### Example 1 — Add text expansion matches

```yaml
extensions:
  espanso:
    settings:
      matches:
        - trigger: ':email'
          replace: 'my.professional.email@example.com'
        - trigger: ':brb'
          replace: 'Be right back!'
```

### Example 2 — Configure global variables and matches together

```yaml
extensions:
  espanso:
    settings:
      global_vars:
        - name: 'current_date'
          type: 'date'
          params:
            format: '%Y-%m-%d'
      matches:
        - trigger: ':today'
          replace: "Today's date is {{current_date}}"
```

### Example 3 - Add a multi-line template (e.g., Email signature or Code boilerplate)

```yaml
extensions:
  espanso:
    settings:
      matches:
        - trigger: ":signature"
          replace: "Best Regards,\nJuhi Gupta\nSoftware Engineer"
        - trigger: ":cpp"
          replace: "#include <iostream>\nusing namespace std;\n\nint main() {\n    return 0;\n}"
```

## Notes / Caveats

- The plugin performs intelligent list merging: items in matches are updated if their trigger matches, and global_vars are updated if their name matches. Any existing items in the native config not mentioned in user's WinHome config are safely preserved.
- It features a built-in YAML parser and serializer, so it does not rely on external dependencies like PyYAML to read or write the base.yml file.
- It supports dryRun mode — when it is enabled, it logs whether changes would be made without actually writing the updates to the disk.
- The plugin performs intelligent list merging specifically for `matches` and `global_vars`.
- Items in `matches` are updated if their `trigger` matches; otherwise, new triggers are appended.
- Items in `global_vars` are updated if their `name` matches; otherwise, new variables are appended.

## Verification Steps

After applying the configuration, user can verify if Espanso has picked up the changes or not.
For this user has to open its terminal and type:

```bash
espanso status
```
