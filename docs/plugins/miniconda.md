# Miniconda Plugin

## Overview

The Miniconda plugin manages configuration settings in the `.condarc` file for the Miniconda and Conda package/environment manager.

## Prerequisites

- Miniconda or Conda installed (`conda` executable in PATH)

## Configuration Schema

Settings accept key-value pairs under the `settings` key. Mapped options include:

| Key | Conda Key | Description |
|-----|-----------|-------------|
| `channels` | `channels` | List of channels to fetch packages from. |
| `channelAlias` | `channel_alias` | Prefix to prepend to channel names. |
| `sslVerify` | `ssl_verify` | Verifies SSL certificates for HTTPS requests. |
| `proxyServers` | `proxy_servers` | Proxy server configuration for channels downloads. |
| `envsDirs` | `envs_dirs` | List of directories where environments are created. |
| `pkgsDirs` | `pkgs_dirs` | List of directories where downloaded packages are stored. |
| `autoUpdateConda` | `auto_update_conda` | Controls whether conda updates itself. |
| `autoActivateBase` | `auto_activate_base` | Activates base env in new shells. |
| `anacondaUpload` | `anaconda_upload` | Controls whether packages are uploaded to Anaconda Client. |
| `reportErrors` | `report_errors` | Controls whether to report errors to Anaconda. |
| `pipInteropEnabled` | `pip_interop_enabled` | Enable/disable pip interoperability. |
| `maxParallelDownloads` | `max_parallel_downloads` | Max concurrent connection downloads. |
| `privateEnvs` | `private_envs` | Enables private environment creation. |
| `modifyPath` | `modify_path` | Controls path modification during conda execution. |

## Usage Examples

```yaml
plugins:
  - name: miniconda
    settings:
      channels:
        - defaults
        - conda-forge
      sslVerify: true
      autoActivateBase: false
```

## Verification Steps

To verify settings, inspect the condarc file:
```bash
cat ~/.condarc
```

## Notes / Caveats

- All settings are modified atomically in `%USERPROFILE%\.condarc`.
- If the configuration fails, a backup of the current `.condarc` is created under `.condarc.bak.*`.
