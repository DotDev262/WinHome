# Go plugin

## Description

The `go` plugin manages Go toolchain environment variables via `go env` and `go env -w`. It reads current values before applying changes and supports dry-run mode.

## Prerequisites

- Go installed and available on `PATH` (`go` or `go.exe`)
- Detected via `shutil.which`

## Configuration source

| Method | Command |
|--------|---------|
| Read | `go env KEY` |
| Write | `go env -w KEY=VALUE` |

User-level overrides are stored in the Go environment file (typically `%USERPROFILE%\.go\env` on Windows).

## Supported settings

| Key | Type | Description |
|-----|------|-------------|
| `GOPATH` | string | Workspace directory for Go projects |
| `GOROOT` | string | Go installation directory |
| `GOOS` | string | Target OS (`windows`, `linux`, `darwin`) |
| `GOARCH` | string | Target architecture (`amd64`, `arm64`) |
| `GO111MODULE` | string | Module mode (`on`, `off`, `auto`) |
| `GOPROXY` | string | Module proxy URL |
| `GONOSUMCHECK` | string | Modules to skip sum check |
| `GONOSUMDB` | string | Module paths to skip sum DB |
| `GOPRIVATE` | string | Private module patterns |

## Usage examples

### Module and proxy defaults

```yaml
extensions:
  go:
    settings:
      GO111MODULE: on
      GOPROXY: https://proxy.golang.org,direct
      GOPRIVATE: github.com/myorg/*
```

### Cross-compile targets

```yaml
extensions:
  go:
    settings:
      GOOS: windows
      GOARCH: amd64
```

## Notes

- Unknown keys in `settings` are skipped with a stderr warning.
- When Go is not installed, `apply` returns `success: false` with a clear error.
- No-op when requested values already match `go env` output.
- Supports `dryRun` in request context.
- Config key in WinHome: `extensions.go`.

## Verification

```powershell
go env GOPATH GO111MODULE GOPROXY
```
