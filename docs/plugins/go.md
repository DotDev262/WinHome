# Go Plugin

## Overview

The Go plugin manages environment variables for the Go language toolchain using `go env -w`.

## Prerequisites

- Go installed and available on system PATH (`go.exe` or `go`)

## Configuration Schema

The plugin accepts environment settings under the `settings` key. Supported variables:

| Key | Description |
|-----|-------------|
| GO111MODULE | Enable/disable modules support |
| GOARCH | Target architecture |
| GONOSUMCHECK | List of module patterns to skip sum checks |
| GONOSUMDB | List of module patterns to skip sum DB verification |
| GOOS | Target operating system |
| GOPATH | Go workspace path |
| GOPRIVATE | Module patterns considered private |
| GOPROXY | Go module proxy URL(s) |
| GOROOT | Go SDK installation path |

## Usage Examples

```yaml
plugins:
  - name: go
    settings:
      GOPROXY: "https://proxy.golang.org,direct"
      GOPRIVATE: "*.corp.example.com"
```

## Verification Steps

```bash
go env
```

## Notes / Caveats

- Go must be present on the system PATH for the plugin to function.
- Configuration settings are modified persistently at the user level via `go env -w`.
