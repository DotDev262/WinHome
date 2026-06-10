# Windows Sandbox Plugin

## Overview

The Windows Sandbox plugin manages settings in the `sandbox.wsb` configuration file for the Windows Sandbox environment.

## Prerequisites

- Windows Sandbox feature enabled (executable at `%WINDIR%\System32\WindowsSandbox.exe` present)

## Configuration Schema

Settings are defined as key-value pairs under the `settings` key:

- Boolean keys (mapped to `Enable` or `Disable` XML elements):
  - `vGPU`: Controls virtual GPU enablement (`VGpu`).
  - `networking`: Controls network access enablement (`Networking`).
  - `audioInput`: Controls host audio input redirection (`AudioInput`).
  - `videoInput`: Controls host video input redirection (`VideoInput`).
  - `protectedClient`: Runs sandbox under extra security mitigations (`ProtectedClient`).
  - `printerRedirection`: Redirects host printers to the sandbox (`PrinterRedirection`).
  - `clipboardRedirection`: Controls host-sandbox clipboard sync (`ClipboardRedirection`).
- Integer settings:
  - `memoryInMB`: Allocates a specific RAM amount (in MB) for the sandbox.
- List settings:
  - `mappedFolders`: List of shared folders containing:
    - `hostFolder`: Host path to share.
    - `readOnly`: If true, makes the shared folder read-only inside the sandbox (defaults to false).

## Usage Examples

```yaml
plugins:
  - name: windows-sandbox
    settings:
      vGPU: true
      networking: true
      memoryInMB: 2048
      mappedFolders:
        - hostFolder: "C:\\Shared"
          readOnly: true
```

## Verification Steps

To verify the sandbox config output:
```bash
cat %USERPROFILE%\Documents\sandbox.wsb
```

## Notes / Caveats

- WSB files are XML-formatted. The plugin automatically generates a default schema with GPU, networking, audio/video input, printer/clipboard redirection enabled, and 1024MB memory if no file exists.
