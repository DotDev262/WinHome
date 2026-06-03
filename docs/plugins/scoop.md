# scoop plugin
 
## Description
 
The `scoop` plugin manages Scoop client configuration on Windows. It deep-merges settings into Scoop's `config.json`, allowing you to declaratively control install paths, download behaviour, proxy settings, and other Scoop options as part of your WinHome setup.
 
This plugin manages Scoop **configuration** only. Use the top-level `scoop:` block in `config.yaml` (or `apps:` entries with `manager: scoop`) for package installation workflows.
 
## Prerequisites
 
- Scoop must be installed (see [scoop.sh](https://scoop.sh))
- Scoop is detected via `scoop.exe`, `scoop.cmd`, `scoop.ps1`, or `scoop` on `PATH`
- **Windows only**
## Configuration file location
 
| Condition | Path |
|-----------|------|
| `XDG_CONFIG_HOME` is set | `%XDG_CONFIG_HOME%\scoop\config.json` |
| Default | `%USERPROFILE%\.config\scoop\config.json` |
 
This matches Scoop's own config resolution logic in `lib/core.ps1`.
 
## Configuration format
 
```yaml
extensions:
  scoop:
    settings:
      <key>: <value>
```
 
Settings are deep-merged into `config.json`. Nested objects merge recursively; scalar values overwrite existing keys.
 
## Supported settings
 
Any valid Scoop config key is supported. Common options include:
 
| Key | Type | Description |
|-----|------|-------------|
| `root_path` | string | Custom Scoop install root (e.g. `D:\scoop`) |
| `global_path` | string | Global install root for `scoop install -g` |
| `cache_path` | string | Download cache directory |
| `proxy` | string | Proxy URL for downloads (e.g. `host:port` or `user:password@host:port`) |
| `aria2-enabled` | boolean | Use aria2c for parallel/faster downloads |
| `aria2-warning-enabled` | boolean | Show aria2 warnings in output |
| `gh_token` | string | GitHub personal access token to avoid API rate limiting |
| `virustotal_api_key` | string | VirusTotal API key for scanning downloaded files |
| `use_lessmsi` | boolean | Use `lessmsi` to extract MSI installers instead of `msiexec` |
| `no_junction` | boolean | Disable directory junctions for app `current` links |
| `debug` | boolean | Enable verbose Scoop debug output |
| `shim` | string | Shim backend to use: `kiennq`, `scoopcs`, or `71` |
| `autostash_on_conflict` | boolean | Automatically stash git changes on update conflicts |
| `use_isolated_path` | boolean | Stop Scoop from modifying the system `PATH` |
 
> Keys not listed here are still accepted — anything valid for `scoop config <key> <value>` can be set.
 
## Usage examples
 
### Example 1 — Custom install and cache directories
 
```yaml
extensions:
  scoop:
    settings:
      root_path: D:\scoop
      cache_path: D:\scoop\cache
```
 
### Example 2 — Enable aria2 downloads and set a GitHub token
 
```yaml
extensions:
  scoop:
    settings:
      aria2-enabled: true
      aria2-warning-enabled: false
      gh_token: ghp_your_personal_access_token_here
```
 
### Example 3 — Corporate proxy with lessmsi
 
```yaml
extensions:
  scoop:
    settings:
      proxy: proxy.corp.example.com:8080
      use_lessmsi: true
```
 
## Notes
 
- The plugin deep-merges settings — existing keys in `config.json` that are not mentioned in `config.yaml` are preserved.
- If `config.json` is corrupted or cannot be parsed, the plugin automatically backs it up with a timestamped unique suffix (e.g. `config.json.corrupted.20240101120000.a1b2c3d4`) and starts fresh.
- Writes use an atomic temp-file replace to avoid partial updates on failure.
- Supports `dryRun` mode — logs the target path and what would change without writing to disk.
- **Windows only** — this plugin has no effect on other operating systems.
## Verification
 
After applying, inspect Scoop's config via the CLI:
 
```powershell
scoop config
```
 
To check a specific key:
 
```powershell
scoop config proxy
scoop config root_path
```
 
Or read the file directly:
 
```powershell
Get-Content "$env:USERPROFILE\.config\scoop\config.json"
```
 
Then run `scoop checkup` to confirm Scoop is happy with the current configuration.
 
