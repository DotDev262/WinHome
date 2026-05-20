# Plugins & Extensions

WinHome supports a powerful plugin system that allows for first-class configuration of tools like Vim and VSCode. These can be defined in their own top-level sections in `config.yaml`.

## Vim / Neovim

The `vim` section allows you to manage plugins and settings for Neovim (`init.lua`).

### Example

```yaml
vim:
  extensions:
    - "tpope/vim-commentary"
    - "nvim-treesitter/nvim-treesitter"
  settings:
    number: true
    relativenumber: true
    theme: "gruvbox"
```

### Options

- `extensions`: A list of GitHub repositories (`user/repo`) to install.
- `settings`: A dictionary of Lua settings to apply to `init.lua`.
  - `theme`: Translates to `vim.cmd('colorscheme <value>')`.
  - `key: value`: Translates to `vim.opt.<key> = <value>`.

---

## VSCode

The `vscode` section allows you to manage extensions and user settings for both the default profile and named profiles.

### Example

```yaml
vscode:
  # Default Profile
  extensions:
    - "dbaeumer.vscode-eslint"
    - "esbenp.prettier-vscode"
  settings:
    "editor.tabSize": 2
    "files.autoSave": "afterDelay"

  # Named Profiles
  profiles:
    "Work":
      extensions:
        - "ms-dotnettools.csdevkit"
      settings:
        "editor.fontSize": 14
    "Personal":
      settings:
        "workbench.colorTheme": "Default Dark Modern"
```

### Options

- `extensions`: A list of VSCode extension IDs to install in the default profile.
- `settings`: A dictionary of settings to merge into the default `settings.json`.
- `profiles`: A dictionary of named profiles to manage.
  - `<profile-name>`:
    - `extensions`: Extensions specific to this profile.
    - `settings`: Settings specific to this profile.

> **Note:** WinHome will automatically create the profile in VSCode if it doesn't exist by adding it to `storage.json`.

---

## Obsidian

The `obsidian` section allows you to manage community plugins and settings across your Obsidian vaults.

### Example

```yaml
obsidian:
  vaults:
    - path: "C:\\Users\\test\\Documents\\TestVault"
      settings:
        spellcheck: true
        accentColor: "#002aff"
      plugins:
        - "obsidian-git"
```

### Options

- `vaults`: A list of Obsidian vaults to configure.
  - `path`: The absolute path to the vault directory.
  - `settings`: A dictionary of settings to merge into `.obsidian/app.json` or `.obsidian/appearance.json`. Supports keys like `spellcheck`, `accentColor`, `theme`, `vimMode`, etc.
  - `plugins`: A list of community plugin IDs to download, install, and enable in the vault.

---

## PowerToys

Configure the `powertoys` plugin under `extensions` to manage PowerToys settings stored in `%LOCALAPPDATA%\Microsoft\PowerToys`.

### Example
```yaml
extensions:
  powertoys:
    general:
      settings:
        theme: 0
    modules:
      fancyzones:
        enabled: true
        settings:
          shiftDrag: true
      awake:
        enabled: true
        settings:
          keepAwake: true
          keepAwakeTimeInMinutes: 30
      powerrename:
        enabled: true
        settings:
          isEnabled: true
```

### Options
- `general`: Merge into `%LOCALAPPDATA%\Microsoft\PowerToys\settings.json`.
  - `settings`: Dictionary merged at the top level.
  - `raw`: Dictionary merged at the top level (advanced).
- `modules`: Dictionary of module configs.
  - `fancyzones`, `awake`, `powerrename`: Supported modules.
  - `enabled`: Sets the module's top-level `enabled` flag.
  - `settings`: Merged into the module's `properties` block.
  - `properties`: Alias for `settings`.
  - `raw`: Merge into the module's top-level JSON (advanced).

## Generic Extensions

For other plugins that do not have a dedicated top-level section, use the `extensions` block.

### Example

```yaml
extensions:
  test-echo:
    message: "Hello from Python Plugin!"
```
