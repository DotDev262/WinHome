# Joplin Plugin

## Overview

This plugin manages the configuration of the Joplin desktop application stored in `%APPDATA%\joplin-desktop\settings.json`. Settings are merged directly into the existing JSON file, allowing Joplin preferences to be managed declaratively through WinHome.

## Prerequisites

* Joplin installed
* Windows system with `%APPDATA%` available
* Permission to write to `%APPDATA%\joplin-desktop`

## Configuration Schema

The plugin accepts a top-level YAML object with a single supported field:

| Field      | Type   | Default | Description                                                                   |
| ---------- | ------ | ------- | ----------------------------------------------------------------------------- |
| `settings` | object | none    | Key-value pairs to merge into `settings.json`. Keys must match Joplin settings. |

### Common Settings Keys

While any valid Joplin settings key can be configured, here are some commonly used keys:

| Key | Type | Description |
| --- | --- | --- |
| `theme` | integer | Active theme ID (e.g., `1` for Light, `2` for Dark, `4` for Cosmic Dark, `22` for OLED Dark). |
| `themeAutoDetect` | boolean | If `true`, Joplin automatically switches the theme to match the system theme. |
| `style.editor.fontSize` | integer | Font size in pixels for the editor (min: `4`, max: `50`). |
| `style.editor.fontFamily` | string | Font family used for most text in the markdown editor. |
| `style.editor.monospaceFontFamily` | string | Monospace font family used for code, tables, checkboxes, etc. |
| `markdown.plugin.mermaid` | boolean | Enable/disable rendering of Mermaid diagrams. |
| `markdown.plugin.katex` | boolean | Enable/disable rendering of KaTeX math equations. |
| `markdown.plugin.toc` | boolean | Enable/disable rendering of Table of Contents. |
| `sync.target` | integer | Sync target ID (e.g., `0` for None, `2` for Local Directory). |
| `sync.2.path` | string | Absolute path to the sync folder (when `sync.target` is set to `2`). |
| `locale` | string | Interface language code (e.g., `"en_US"`). |

## Usage Examples

### Configure editor theme and font styling

```yaml
extensions:
  joplin:
    settings:
      theme: 2
      themeAutoDetect: false
      "style.editor.fontSize": 16
      "style.editor.fontFamily": "Segoe UI"
      "style.editor.monospaceFontFamily": "Fira Code"
```

### Toggle markdown plugins

```yaml
extensions:
  joplin:
    settings:
      "markdown.plugin.mermaid": true
      "markdown.plugin.katex": true
      "markdown.plugin.toc": true
```

### Configure local directory synchronization

```yaml
extensions:
  joplin:
    settings:
      "sync.target": 2
      "sync.2.path": "C:\\Users\\Username\\Dropbox\\Joplin"
```

## Verification Steps

1. Apply your WinHome configuration.
2. Open `%APPDATA%\joplin-desktop\settings.json`.
3. Verify the expected keys were added or updated.
4. Launch Joplin and confirm the settings are reflected in Tools > Options.

## Notes / Caveats

* Existing configuration settings are preserved unless explicitly overwritten by the plugin.
* Unknown keys are written as provided without validation.
* Only Windows is supported (specifically target directories and default installation paths).
* Ensure Joplin is closed before applying configuration changes to prevent it from overwriting `settings.json` upon exit.
* Supports `dryRun` mode.
