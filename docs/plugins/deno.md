# Deno Plugin
## Overview
The Deno plugin manages configuration for the Deno runtime by reading and writing settings to your project's `deno.json` (or `deno.jsonc`) file.
## Prerequisites
- Deno installed
- Available in PATH
- Windows 10 or later
## Configuration Schema
| Key | Purpose |
| --- | ------- |
| importMap | Path to an import map file |
| compilerOptions | TypeScript compiler options |
| lint | Linting configuration |
| fmt | Code formatting configuration |
| tasks | Deno task definitions |
| nodeModulesDir | Whether to use a node_modules directory |
| unstable | Unstable feature flags |
| vendor | Vendoring configuration |
| permissions | Default permissions |
| publish | Publishing configuration |
| lock | Lockfile configuration |
| typeCheckOnRun | Enable type checking on deno run |
| watch | File watching configuration |
## Usage Examples
### Basic formatter and linter config
```yaml
extensions:
  deno:
    settings:
      fmt:
        indentWidth: 2
        singleQuote: true
      lint:
        rules:
          tags:
            - recommended
```
### With import map and task definitions
```yaml
extensions:
  deno:
    settings:
      importMap: "./import_map.json"
      tasks:
        start: "deno run --allow-net main.ts"
        test: "deno test --allow-read"
```
### Using node_modules and unstable features
```yaml
extensions:
  deno:
    settings:
      nodeModulesDir: true
      unstable:
        - "bare-node-builtins"
        - "sloppy-imports"
```
## Verification Steps
```powershell
deno --version
```
## Notes / Caveats
- Only the predefined set of Deno settings listed above are supported; unknown keys are ignored.
- The plugin automatically backs up your existing `deno.json` before making any changes.
- If both `deno.json` and `deno.jsonc` exist, `deno.json` takes priority.
- This plugin only manages `deno.json` settings — it does not install Deno itself.
- Windows only: this plugin is part of WinHome, a Windows developer environment tool.
