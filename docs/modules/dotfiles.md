# Dotfiles

Creates symbolic links for your dotfiles.

**YAML Key:** `dotfiles`

**Properties:**
-   `src`: The source file path.
-   `target`: The target link path. Environment variables and `~` are supported.

**Example:**
```yaml
dotfiles:
  - src: dotfiles/.gitconfig
    target: ~/.gitconfig
  - src: dotfiles/.zshrc
    target: ~/.zshrc
```
