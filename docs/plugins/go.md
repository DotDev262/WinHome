# Go Plugin

## Overview

The Go plugin manages configuration for the Go environment variables using the `go env` command.

## Prerequisites

- Go installed
- Available in PATH

## Configuration Schema

| Key | Purpose |
| --- | ------- |
| GO111MODULE | Module support |
| GOARCH | Target architecture |
| GONOSUMDB | Skip checksum database |
| GOOS | Target operating system |
| GOPATH | Go workspace path |
| GOPRIVATE | Private modules |
| GOPROXY | Go module proxy |
| GOROOT | Go installation root |

## Usage Examples

### Go config

```yaml
extensions:
  go:
    settings:
      GOPROXY: "https://proxy.golang.org,direct"
      GO111MODULE: "on"
```

## Verification Steps

```bash
go env
```

## Notes / Caveats

- Only a predefined set of Go environment variables are supported.
- Modifies the Go environment directly via `go env -w`.
