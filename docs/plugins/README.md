
# Plugin Directory

WinHome currently ships with 41 built-in plugins under `plugins/`. This page acts as a
marketplace-style index for those plugins and a quick reference for how each one is enabled from
`config.yaml`.

## Capability Legend

- `config_provider`: The plugin reads a config section and reconciles one or more tool-specific
  config files.
- `package_manager`: The plugin can also back an `apps:` entry by implementing `check_installed`,
  `install`, and `uninstall`.

## At A Glance

### Package And Ecosystem

| Name         | Brief description                                                       | Capabilities      | Docs                      |
| ------------ | ----------------------------------------------------------------------- | ----------------- | ------------------------- |
| `cargo`      | Manages Cargo settings in `.cargo/config.toml`.                         | `config_provider` | [Details](#cargo)         |
| `chocolatey` | Manages Chocolatey client configuration and feature flags.              | `config_provider` | [Details](#chocolatey)    |
| `miniconda`  | Python/R Package and Environment Manager                                | `config_provider` | [Details](./miniconda.md) |
| `npm`        | Manages user-level `.npmrc` settings.                                   | `config_provider` | [Details](#npm)           |
| `pip`        | Manages `pip.ini` settings for Python package installs.                 | `config_provider` | [Details](#pip)           |
| `scoop`      | Command-line Installer Provisioning Framework                           | `config_provider` | [Details](./scoop.md)     |
| `sdkman`     | Software Development Kit Manager for Java Ecosystem                     | `config_provider` | [Details](./sdkman.md)    |
| `syncthing`  | Continuous File Synchronization Framework                               | `config_provider` | [Details](./syncthing.md) |
| `winget`     | Manages Winget CLI `settings.json`, separate from package installation. | `config_provider` | [Details](#winget)        |
| `7-zip`      | High-Ratio File Archiver and Compression Tool                           | `config_provider` | [Details](./7-zip.md)     |

### Editors And Knowledge Tools

| Name              | Brief description                                             | Capabilities                         | Docs                        |
| ----------------- | ------------------------------------------------------------- | ------------------------------------ | --------------------------- |
| `helix-editor`    | Manages Helix `config.toml` and `languages.toml`.             | `config_provider`                    | [Details](#helix-editor)    |
| `notepadplusplus` | Manages Notepad++ JSON settings.                              | `config_provider`                    | [Details](#notepadplusplus) |
| `obsidian`        | Configures vault settings and community plugins for Obsidian. | `config_provider`                    | [Details](#obsidian)        |
| `vim`             | Generates `init.lua` and installs Neovim plugins from GitHub. | `config_provider`, `package_manager` | [Details](#vim)             |
| `vscode`          | Syncs VS Code settings, profiles, and extensions.             | `config_provider`, `package_manager` | [Details](#vscode)          |
| `zed`             | Manages Zed editor settings in JSONC format.                  | `config_provider`                    | [Details](#zed)             |

### Shell, Terminal, And Navigation

| Name               | Brief description                                                                         | Capabilities      | Docs                         |
| ------------------ | ----------------------------------------------------------------------------------------- | ----------------- | ---------------------------- |
| `alacritty`        | Manages Alacritty `alacritty.toml` terminal settings.                                     | `config_provider` | [Details](#alacritty)        |
| `bat`              | Manages `bat` syntax-highlighting pager configuration.                                    | `config_provider` | [Details](#bat)              |
| `ohmyposh`         | Manages the Oh My Posh bootstrap line in a PowerShell profile.                            | `config_provider` | [Details](#ohmyposh)         |
| `powershell`       | Generates a managed PowerShell profile block for aliases, modules, prompt, and functions. | `config_provider` | [Details](#powershell)       |
| `starship`         | Manages `starship.toml` prompt settings.                                                  | `config_provider` | [Details](#starship)         |
| `windows-terminal` | Manages Windows Terminal `settings.json` for stable, preview, or dev installs.            | `config_provider` | [Details](#windows-terminal) |
| `yazi`             | Manages Yazi core, keymap, and theme TOML files.                                          | `config_provider` | [Details](#yazi)             |
| `zoxide`           | Adds or updates shell init hooks for Zoxide in PowerShell and Bash profiles.              | `config_provider` | [Details](#zoxide)           |

### Developer Workflow And Infrastructure

| Name       | Brief description                                            | Capabilities      | Docs                 |
| ---------- | ------------------------------------------------------------ | ----------------- | -------------------- |
| `chezmoi`  | Manages chezmoi dotfile manager configuration.               | `config_provider` | [Details](#chezmoi)  |
| `curl`     | Manages `_curlrc` settings for curl.                         | `config_provider` | [Details](#curl)     |
| `docker`   | Manages Docker Desktop `settings.json`.                      | `config_provider` | [Details](#docker)   |
| `gh`       | Manages GitHub CLI `config.yml`.                             | `config_provider` | [Details](#gh)       |
| `gh-dash`  | Manages `gh-dash` dashboard settings in `config.yml`.        | `config_provider` | [Details](#gh-dash)  |
| `lazydocker` | Manages LazyDocker YAML configuration.                     | `config_provider` | [Details](#lazydocker) |
| `lazygit`  | Manages `lazygit` YAML configuration.                        | `config_provider` | [Details](#lazygit)  |
| `mise`     | Manages `config.toml` for the mise version manager.          | `config_provider` | [Details](#mise)     |
| `opencode` | Manages OpenCode JSON and JSONC settings.                    | `config_provider` | [Details](#opencode) |
| `openssh`  | Manages global and host-specific entries in `~/.ssh/config`. | `config_provider` | [Details](#openssh)  |
| `rclone`   | Manages `rclone.conf` global settings and remotes.           | `config_provider` | [Details](#rclone)   |

<a id="rustup"></a>

#### rustup

Config key: `extensions.rustup`

Deep-merges TOML settings into `%USERPROFILE%\.rustup\settings.toml`.

### Automation, Productivity, And Desktop Utilities

| Name         | Brief description                                                           | Capabilities      | Docs                   |
| ------------ | --------------------------------------------------------------------------- | ----------------- | ---------------------- |
| `autohotkey` | Manages an AutoHotkey v2 bootstrap script and WinHome-owned settings block. | `config_provider` | [Details](#autohotkey) |
| `espanso`    | Manages Espanso text expansion rules in `base.yml`.                         | `config_provider` | [Details](#espanso)    |
| `everything` | Manages Everything search application configuration.                        | `config_provider` | [Details](#everything) |
| `keepassxc`  | Manages KeePassXC INI settings.                                             | `config_provider` | [Details](#keepassxc)  |
| `powertoys`  | Manages PowerToys general settings and supported module settings.           | `config_provider` | [Details](#powertoys)  |
| `sharex`     | Manages ShareX `ShareX.json`.                                               | `config_provider` | [Details](#sharex)     |
| `rustup`     | Manages `settings.toml` for the Rust toolchain installer.                   | `config_provider` | [Details](#rustup)     |
| `windows-explorer` | Manages Windows Explorer preferences and settings.                    | `config_provider` | [Details](#windows-explorer) |
| `windows-sandbox` | Manages Windows Sandbox `.wsb` configuration settings.                 | `config_provider` | [Details](#windows-sandbox) |
| `yasb` | Manages YASB status bar YAML configuration.                                       | `config_provider` | [Details](#yasb) |

### Community And Communication

| Name            | Brief description                                       | Capabilities      | Docs                      |
| --------------- | ------------------------------------------------------- | ----------------- | ------------------------- |
| `betterdiscord` | Manages BetterDiscord data settings in `settings.json`. | `config_provider` | [Details](#betterdiscord) |
| `discord`       | Manages Discord client settings in `settings.json`.     | `config_provider` | [Details](#discord)       |

### Examples And Test Fixtures

| Name              | Brief description                                                            | Capabilities                         | Docs                        |
| ----------------- | ---------------------------------------------------------------------------- | ------------------------------------ | --------------------------- |
| `test-powershell` | Sample PowerShell plugin used to exercise both plugin capabilities in tests. | `config_provider`, `package_manager` | [Details](#test-powershell) |

## Enabling Plugins In `config.yaml`

WinHome discovers plugin manifests from `plugins/`, but it only boots the runtime for plugins that
are actually referenced from `config.yaml`.

By default, built-in plugins are enabled from the `extensions:` block:

```yaml
extensions:
  powershell:
    settings:
      aliases:
        ll: Get-ChildItem
```

Four plugins also have first-class top-level aliases wired into the engine, so either of these forms
works:

- `vim`
- `vscode`
- `obsidian`
- `ohmyposh`

```yaml
vscode:
  settings:
    'editor.tabSize': 2
  extensions:
    - ms-python.python
```

Only plugins that advertise `package_manager` can be used from `apps:`. In this repository those are
`vim`, `vscode`, and `test-powershell`.

```yaml
apps:
  - id: 'tpope/vim-fugitive'
    manager: 'vim'
```

Two names are easy to confuse with built-in package modules:

- Use top-level `winget:` or `chocolatey:` for package installation workflows.
- Use `extensions.winget` or `extensions.chocolatey` for plugin-managed CLI configuration files.

## How To Contribute A New Plugin

1. Create a new folder under `plugins/<plugin-name>/` with a `plugin.yaml` manifest.
2. Implement the JSON-over-stdin/stdout protocol described in the [plugin tutorial](tutorial.md),
   and support `apply` at minimum.
3. Add tests under `test/` or `tests/` that cover normal apply, idempotency, dry-run behavior, and
   error handling.
4. Add the plugin to this directory page in the right category and give it a detail section.
5. Open a pull request using the repository contribution guide.

Helpful references:

- [Plugin tutorial](tutorial.md)
- [Plugin development guide](development-guide.md)
- [Plugin protocol specification](../architecture/plugin_spec_v1.md)

## Plugin Details

### Package And Ecosystem

<a id="cargo"></a>

#### cargo

Config key: `extensions.cargo`

Merges Cargo settings into `%USERPROFILE%\.cargo\config.toml`.

<a id="chocolatey"></a>

#### chocolatey

Config key: `extensions.chocolatey`

Manages `%ChocolateyInstall%\config\chocolatey.config`, including both `config` values and
`features`. For package installs, see [Chocolatey module docs](../modules/chocolatey.md).

<a id="npm"></a>

#### npm

Config key: `extensions.npm`

Merges key-value settings into the current user's `.npmrc`.

<a id="pip"></a>

#### pip

Config key: `extensions.pip`

Merges Pip settings into `%APPDATA%\pip\pip.ini`.

<a id="winget"></a>

#### winget

Config key: `extensions.winget`

Merges Winget CLI settings into
`%LOCALAPPDATA%\Packages\Microsoft.DesktopAppInstaller_8wekyb3d8bbwe\LocalState\settings.json`. For
package installs, see [Winget module docs](../modules/winget.md).

### Editors And Knowledge Tools

<a id="helix-editor"></a>

#### helix-editor

Config key: `extensions.helix-editor`

Manages both `%APPDATA%\helix\config.toml` and `%APPDATA%\helix\languages.toml`.

<a id="notepadplusplus"></a>

#### notepadplusplus

Config key: `extensions.notepadplusplus`

Merges JSON settings into `%APPDATA%\Notepad++\config.json`.

<a id="obsidian"></a>

#### obsidian

Config key: `extensions.obsidian` or top-level `obsidian`

Configures vault-scoped Obsidian settings and can install or enable community plugins inside
`.obsidian`.

<a id="vim"></a>

#### vim

Config key: `extensions.vim` or top-level `vim`

Generates `%LOCALAPPDATA%\nvim\init.lua` from `settings` and can install Git-based Neovim plugins
under `%LOCALAPPDATA%\nvim-data\site\pack\winhome\start`.

<a id="vscode"></a>

#### vscode

Config key: `extensions.vscode` or top-level `vscode`

Syncs VS Code `settings.json`, named profiles, and extension installation state under
`%APPDATA%\Code\User`.

<a id="zed"></a>

#### zed

Config key: `extensions.zed`

Merges JSONC settings into `%APPDATA%\Zed\settings.json` with automatic type coercion for int and
bool keys.

### Shell, Terminal, And Navigation

<a id="alacritty"></a>

#### alacritty

Config key: `extensions.alacritty`

Merges TOML settings into `%APPDATA%\alacritty\alacritty.toml`.

<a id="bat"></a>

#### bat

Config key: `extensions.bat`

Manages flags and variables in `%APPDATA%\bat\config`.

<a id="ohmyposh"></a>

#### ohmyposh

Config key: `extensions.ohmyposh` or top-level `ohmyposh`

Ensures a PowerShell profile contains the expected `oh-my-posh init` line for the selected theme.

<a id="powershell"></a>

#### powershell

Config key: `extensions.powershell`

Generates a WinHome-managed profile block for aliases, modules, prompt settings, PSReadLine options,
and custom functions.

<a id="starship"></a>

#### starship

Config key: `extensions.starship`

Merges prompt settings into `%USERPROFILE%\.config\starship.toml`.

<a id="windows-terminal"></a>

#### windows-terminal

Config key: `extensions.windows-terminal`

Merges settings into the active Windows Terminal `settings.json`, including stable, preview, dev, or
portable locations.

<a id="yazi"></a>

#### yazi

Config key: `extensions.yazi`

Splits settings across `%APPDATA%\yazi\config\yazi.toml`, `keymap.toml`, and `theme.toml`.

<a id="zoxide"></a>

#### zoxide

Config key: `extensions.zoxide`

Adds or refreshes Zoxide initialization lines in PowerShell and Bash profile files.

### Developer Workflow And Infrastructure

<a id="chezmoi"></a>

#### chezmoi

Config key: `extensions.chezmoi`

Deep-merges YAML settings into `%LOCALAPPDATA%\chezmoi\chezmoi.yaml`.

<a id="curl"></a>

#### curl

Config key: `extensions.curl`

Manages proxy, flags, and variables in `%USERPROFILE%\_curlrc`.

<a id="docker"></a>

#### docker

Config key: `extensions.docker`

Deep-merges Docker Desktop settings into `%APPDATA%\Docker\settings.json`.

<a id="gh"></a>

#### gh

Config key: `extensions.gh`

Merges YAML settings into `%APPDATA%\GitHub CLI\config.yml`.

<a id="gh-dash"></a>

#### gh-dash

Config key: `extensions.gh-dash`

Merges dashboard settings into `%USERPROFILE%\.config\gh-dash\config.yml`.

<a id="lazydocker"></a>

#### lazydocker

Config key: `extensions.lazydocker`

Deep-merges YAML settings into `%APPDATA%\lazydocker\config.yml`.

<a id="lazygit"></a>

#### lazygit

Config key: `extensions.lazygit`

Merges YAML settings into `%APPDATA%\lazygit\config.yml`.

<a id="mise"></a>

#### mise

Config key: `extensions.mise`

Deep-merges TOML settings into `%LOCALAPPDATA%\mise\config.toml`.

<a id="opencode"></a>

#### opencode

Config key: `extensions.opencode`

Merges OpenCode JSON or JSONC settings into `%USERPROFILE%\.config\opencode\opencode.json`, or an
explicit project path when provided.

<a id="openssh"></a>

#### openssh

Config key: `extensions.openssh`

Merges global settings and `hosts` blocks into `~/.ssh/config`.

<a id="rclone"></a>

#### rclone

Config key: `extensions.rclone`

Merges global settings and remote definitions into `%USERPROFILE%\.config\rclone\rclone.conf`.

<a id="rustup"></a>

#### rustup

Config key: `extensions.rustup`

Deep-merges TOML settings into `%USERPROFILE%\.rustup\settings.toml`.

### Automation, Productivity, And Desktop Utilities

<a id="autohotkey"></a>

#### autohotkey

Config key: `extensions.autohotkey`

Generates or updates an AutoHotkey v2 script with a WinHome-managed settings block and optional
custom script content.

<a id="espanso"></a>

#### espanso

Config key: `extensions.espanso`

Merges Espanso config into `%APPDATA%\espanso\match\base.yml`.

<a id="everything"></a>

#### everything

Config key: `extensions.everything`

Manages supported Everything configuration settings through WinHome.

<a id="keepassxc"></a>

#### keepassxc

Config key: `extensions.keepassxc`

Merges INI-style settings into `%APPDATA%\KeePassXC\keepassxc.ini`.

<a id="powertoys"></a>

#### powertoys

Config key: `extensions.powertoys`

Manages general PowerToys settings plus supported module files such as `FancyZones`, `Awake`, and
`PowerRename`.

<a id="sharex"></a>

#### sharex

Config key: `extensions.sharex`

Deep-merges ShareX settings into `%APPDATA%\ShareX\ShareX.json`.

<a id="windows-explorer"></a>

#### windows-explorer

Config key: `extensions.windows-explorer`

Manages supported Windows Explorer settings and preferences through WinHome configuration.

<a id="windows-sandbox"></a>

#### windows-sandbox

Config key: `extensions.windows-sandbox`

Manages `%USERPROFILE%\Documents\sandbox.wsb`, including memory allocation, networking, device redirection, clipboard access, and mapped folders.

<a id="yasb"></a>

#### yasb

Config key: `extensions.yasb`

Deep-merges YAML settings into `%USERPROFILE%\.config\yasb\config.yaml`.

### Community And Communication

<a id="betterdiscord"></a>

#### betterdiscord

Config key: `extensions.betterdiscord`

Deep-merges settings into `%APPDATA%\BetterDiscord\data\settings.json`.

<a id="discord"></a>

#### discord

Config key: `extensions.discord`

Deep-merges settings into `%APPDATA%\discord\settings.json`.

### Examples And Test Fixtures

<a id="test-powershell"></a>

#### test-powershell

Config key: `extensions.test-powershell`

Example plugin used by the test suite to validate both `config_provider` and `package_manager`
behavior. It is useful as a reference implementation, not as an end-user integration. See
[full docs](test-powershell.md).
