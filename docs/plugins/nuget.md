# NuGet Plugin

## Overview

The NuGet plugin manages NuGet's machine-wide configuration file (`NuGet.Config`) by merging
package sources, API keys, disabled/fallback sources, repository paths, and general config values
into it. It parses the existing XML file, applies only the settings you specify, and leaves
anything else already in the file untouched.

## Prerequisites

- Windows, with the `APPDATA` environment variable available
- Either the `nuget` or `dotnet` CLI available on `PATH` — `check_installed` also passes if
  `NuGet.Config` already exists at the default path, even without either CLI installed

## Configuration Schema

The plugin accepts a top-level YAML object with these fields, all optional:

| Field | Type | Description |
| --- | --- | --- |
| `packageSources` | list of `{ name, source }` | NuGet feeds to add/update under `<packageSources>`. |
| `apiKeys` | list of `{ key, source }` | API keys to add/update under `<apikeys>`. `key` is the API key value, `source` is the feed URL it applies to. |
| `disabledPackageSources` | list of strings | Source names to disable under `<disabledPackageSources>`. |
| `fallbackPackageSources` | list of `{ name, source }` | Feeds to add/update under `<fallbackPackageSources>`. |
| `repositoryPaths` | list of strings | Paths to add under `<repositoryPaths>`. |
| `globalPackagesFolder` | string | Sets the `globalPackagesFolder` key under `<config>`. |
| `httpProxy` | string | Sets the `httpProxy` key under `<config>`. |
| `httpsProxy` | string | Sets the `httpsProxy` key under `<config>`. |
| `maxHttpRequestsPerSource` | number | Sets the `maxHttpRequestsPerSource` key under `<config>`. |
| `signatureValidationMode` | string | Sets the `signatureValidationMode` key under `<config>`. |

The plugin also supports WinHome's `dryRun` apply option, which reports whether `NuGet.Config`
would change without writing it.

## Usage Examples

### Example 1 — Add a package source

```yaml
extensions:
  nuget:
    settings:
      packageSources:
        - name: nuget.org
          source: https://api.nuget.org/v3/index.json
        - name: company-feed
          source: https://nuget.mycompany.com/v3/index.json
```

### Example 2 — Set the global packages folder and a proxy

```yaml
extensions:
  nuget:
    settings:
      globalPackagesFolder: 'D:\nuget-packages'
      httpProxy: 'http://proxy.mycompany.com:8080'
      maxHttpRequestsPerSource: 16
```

### Example 3 — Disable a source and add an API key for a private feed

```yaml
extensions:
  nuget:
    settings:
      disabledPackageSources:
        - nuget.org
      apiKeys:
        - source: https://nuget.mycompany.com/v3/index.json
          key: 'your-api-key-here'
```

## Verification Steps

```powershell
Test-Path "$env:APPDATA\NuGet\NuGet.Config"
Get-Content "$env:APPDATA\NuGet\NuGet.Config"
```

Confirm the `<packageSources>`, `<config>`, `<apikeys>`, `<disabledPackageSources>`, or
`<fallbackPackageSources>` elements contain the values you configured. You can also run:

```powershell
nuget sources list
```

(or `dotnet nuget list source`) to confirm the package sources are recognized by the NuGet/dotnet
CLI itself.

## Notes / Caveats

- This plugin targets Windows only — it resolves NuGet's config through `%APPDATA%`.
- The XML file is written atomically: a temp file is written in the same directory first, then
  swapped into place with `os.replace`, so a crash mid-write won't corrupt the existing file.
- If the existing `NuGet.Config` can't be parsed (corrupted), the plugin backs it up to
  `NuGet.Config.corrupted.<random-uuid>.bak` before writing a fresh file from your `settings`.
- `apiKeys` entries are keyed by `source` (the feed URL), not by the `key` value itself — if two
  entries share the same `source`, the later one in your list wins.
- `check_installed` returns `true` if either the `nuget`/`dotnet` CLI is on `PATH`, or if
  `NuGet.Config` already exists — it doesn't otherwise verify NuGet is fully functional.
