# WinHome

WinHome is a declarative, portable, and idempotent Windows environment setup tool. It is inspired by tools like Ansible and Puppet, but with a focus on simplicity and ease of use for a single user's machine.

## How it works

WinHome reads a `config.yaml` file that defines the desired state of your system. It then uses a set of services to make your system match that state. It keeps track of the changes it makes in a `winhome.state.json` file, so it can undo them later if you remove them from your config.

## Features

-   **Declarative:** Define your desired state in a single YAML file.
-   **Idempotent:** Running WinHome multiple times with the same config will not cause any changes after the first run.
-   **Package Management:** Install and manage packages from:
    -   Winget
    -   Chocolatey
    -   Scoop
    -   Mise
-   **Dotfile Management:** Symlink your dotfiles from a central repository.
-   **System Configuration:**
    -   Apply registry tweaks.
    -   Set environment variables.
    -   Configure Git.
    -   Manage WSL distributions.
-   **Dry Run Mode:** Preview the changes WinHome will make without actually making them.
-   **Profiles:** Define different profiles for different contexts (e.g., work, personal).

## Usage

```bash
WinHome.exe [options]
```

### Options

-   `--config <path>`: Path to the YAML configuration file. Defaults to `config.yaml`.
-   `--dry-run, -d`: Preview changes without applying them.
-   `--profile <name>, -p <name>`: Activate a specific profile.
-   `--debug`: Enable verbose logging and configuration validation.

## Configuration

The `config.yaml` file has the following sections:

-   `version`: The version of the config file format.
-   `apps`: A list of applications to install.
-   `dotfiles`: A list of dotfiles to symlink.
-   `envVars`: A list of environment variables to set.
--  `registryTweaks`: A list of registry tweaks to apply.
-   `systemSettings`: A list of pre-defined system settings to apply.
-   `git`: Your Git user name and email.
-   `wsl`: WSL configuration.
-   `profiles`: A list of profiles that can be activated with the `--profile` option.

### Example `config.yaml`

```yaml
version: 1.0
apps:
  - manager: winget
    id: Microsoft.PowerToys
  - manager: choco
    id: 7zip
  - manager: scoop
    id: neovim
  - manager: mise
    id: python@3.10

dotfiles:
  - source: C:\Users\user\dotfiles\.gitconfig
    target: C:\Users\user\.gitconfig

envVars:
  - name: MY_VAR
    value: my_value

registryTweaks:
  - path: HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced
    name: HideFileExt
    value: 0
    type: dword

systemSettings:
  - ShowFileExtensions

git:
  userName: "John Doe"
  userEmail: "john.doe@example.com"

wsl:
  update: true
  distros:
    - Ubuntu-20.04

profiles:
  work:
    git:
      userName: "John Doe (Work)"
      userEmail: "john.doe@work.com"
```

## State File

WinHome creates a `winhome.state.json` file in the same directory as the executable. This file is used to keep track of the changes WinHome has made to your system. If you remove an application or a registry tweak from your `config.yaml`, WinHome will use the state file to undo the change.
