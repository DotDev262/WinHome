# Audacity Plugin

## Overview

The Audacity plugin manages configuration settings in the `audacity.cfg` preferences file for the Audacity audio editor.

## Prerequisites

- Audacity installed and available on system PATH (`audacity.exe`) or settings folder present at `%APPDATA%\audacity`

## Configuration Schema

The plugin deep merges key-value pairs specified under the `settings` key.

- Keys follow a `section/key` format to specify settings in specific configuration sections.
- Keys without a slash are added to the root level of the configuration.
- Boolean values are automatically cast to `"1"` (True) or `"0"` (False) in the configuration file.

## Usage Examples

```yaml
plugins:
  - name: audacity
    settings:
      AudioIO/RecordingDevice: "Microphone (Yeti Stereo Microphone)"
      AudioIO/PlaybackDevice: "Speakers (Realtek Audio)"
      Locale/Language: "en"
```

## Verification Steps

To verify the installation:
```bash
audacity --version
```
To verify settings, examine the audacity configuration file:
```bash
cat %APPDATA%\audacity\audacity.cfg
```

## Notes / Caveats

- All settings are modified persistently in `%APPDATA%\audacity\audacity.cfg`.
- If the configuration file does not exist, the plugin creates it and populates the specified keys.
