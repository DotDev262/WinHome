# Shell Completion

WinHome supports tab completion for all CLI flags, subcommands, and file paths in both **PowerShell** and **Bash**.

## Quick Setup

### PowerShell

To enable tab completion for the current session:

```powershell
WinHome completion powershell | Out-String | Invoke-Expression
```

To enable it permanently, save the script to a file and source it in your PowerShell profile:

```powershell
# 1. Save the completion script
WinHome completion powershell > ~/winhome-completion.ps1

# 2. Add to your profile
Add-Content -Path $PROFILE -Value '. ~/winhome-completion.ps1'
```

> **Note:** You may need to restart your PowerShell session or run `. $PROFILE` to reload the profile.

### Bash (WSL / Git Bash)

To enable tab completion for the current session:

```bash
eval "$(WinHome completion bash)"
```

To enable it permanently, save the script and source it in your `~/.bashrc`:

```bash
# 1. Save the completion script
WinHome completion bash > ~/.winhome-completion.bash

# 2. Add to your bashrc
echo "source ~/.winhome-completion.bash" >> ~/.bashrc

# 3. Reload
source ~/.bashrc
```

## What Gets Completed

Once enabled, pressing `Tab` will auto-complete:

| Context               | Completions                                                                 |
|-----------------------|-----------------------------------------------------------------------------|
| `WinHome <TAB>`       | `--config`, `--dry-run`, `--profile`, `--debug`, `--diff`, `--verbose`, `--quiet`, `--json`, `--update`, `state`, `generate`, `completion` |
| `WinHome state <TAB>` | `list`, `backup`, `restore`                                                 |
| `WinHome generate <TAB>` | `--output`, `-o`, `--verbose`, `--quiet`                                 |
| `WinHome --config <TAB>` | File path completion (`.yaml` / `.yml` files in Bash)                    |
| `WinHome state backup <TAB>` | File path completion                                                |

## Supported Shells

| Shell      | Argument     | Mechanism                      |
|------------|-------------|--------------------------------|
| PowerShell | `powershell` | `Register-ArgumentCompleter`   |
| Bash       | `bash`       | `complete -F`                  |

## Troubleshooting

### PowerShell: Completions not working

1. Ensure your execution policy allows profile scripts:
   ```powershell
   Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
   ```
2. Verify the completion script is in your profile:
   ```powershell
   Get-Content $PROFILE | Select-String "WinHome"
   ```

### Bash: `_init_completion` not found

The Bash completion script uses the `bash-completion` package. Install it if missing:

```bash
# Ubuntu / Debian
sudo apt install bash-completion

# macOS (via Homebrew)
brew install bash-completion
```

Then ensure your `~/.bashrc` sources it:

```bash
source /etc/bash_completion
```
