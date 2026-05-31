# openssh plugin

## Description

The `openssh` plugin manages OpenSSH client configuration on Windows. It merges global directives and per-host blocks into `~/.ssh/config`, preserving comments and formatting where possible.

## Prerequisites

- OpenSSH client available as `ssh.exe` or `ssh` on `PATH` (built into modern Windows)
- Write access to the user's `.ssh` directory

## Configuration file location

| Platform | Path |
|----------|------|
| Windows  | `%USERPROFILE%\.ssh\config` |

The plugin resolves the home directory from `~`, `HOME`, or `USERPROFILE`.

## Configuration format

```yaml
extensions:
  openssh:
    global:
      <Directive>: <value>
    hosts:
      <HostName>:
        <Directive>: <value>
```

| Field | Description |
|-------|-------------|
| `global` | Directives applied before any `Host` block (e.g. `IdentityFile`, `AddKeysToAgent`) |
| `hosts` | Map of `Host` pattern → host-specific directives |

## Merge behavior

- **Global:** Keys in `global` update or append directives in the preamble (before the first `Host` block).
- **Hosts:** Each `hosts` entry targets blocks whose `Host` value matches case-insensitively. Missing host blocks are created. Existing directives with the same name are updated; new directives are appended.
- The `Host` directive itself is not overwritten from nested host settings.

## Supported settings

Any valid OpenSSH config directive is supported. Common examples:

| Directive | Example | Description |
|-----------|---------|-------------|
| `HostName` | `github.com` | Real hostname to connect to |
| `User` | `git` | Remote username |
| `IdentityFile` | `~/.ssh/id_ed25519` | Private key path |
| `Port` | `2222` | SSH port |
| `ForwardAgent` | `yes` | Enable agent forwarding |
| `StrictHostKeyChecking` | `accept-new` | Host key policy |

## Usage examples

### Global agent and identity defaults

```yaml
extensions:
  openssh:
    global:
      AddKeysToAgent: "yes"
      IdentityFile: ~/.ssh/id_ed25519
```

### GitHub host alias

```yaml
extensions:
  openssh:
    hosts:
      github.com:
        HostName: github.com
        User: git
        IdentityFile: ~/.ssh/id_ed25519_github
```

### Jump host / bastion

```yaml
extensions:
  openssh:
    hosts:
      prod:
        HostName: 10.0.0.5
        User: deploy
        ProxyJump: bastion.example.com
```

## Notes

- The `.ssh` directory is created with mode `0700`; config files are written with mode `0600` (best-effort on Windows ACLs).
- Supports `dryRun` mode — logs the target path without writing.
- Config key in WinHome: `extensions.openssh`.
- Test with `ssh -G <host>` to inspect the effective configuration.

## Verification

After applying:

```powershell
Get-Content "$env:USERPROFILE\.ssh\config"
ssh -G github.com
```

Confirm global and host blocks contain your directives, then connect to a host to validate behavior.
