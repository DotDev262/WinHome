# WinHome Plugin Architecture Specification

## 1. Architecture Overview

To maintain the **single-file, dependency-free** nature of WinHome while allowing unlimited extensibility, we are adopting a **Process-Based Plugin Architecture**.

Instead of loading dynamic libraries (DLLs)—which is problematic for single-file .NET applications due to trimming and assembly isolation constraints—WinHome will execute plugins as **independent child processes**.

### Why IPC over Standard I/O?
1.  **Language Agnostic**: Plugins can be written in any language (Python, Rust, Go, PowerShell, Node.js).
2.  **Isolation**: A plugin crash cannot bring down the main application.
3.  **Compatibility**: Single-file .NET executables cannot easily load external assemblies dynamically. This bypasses that limitation entirely.

## 2. Communication Protocol

Communication occurs via **Inter-Process Communication (IPC)** using standard input/output streams.

*   **Transport**: The Host (`WinHome.exe`) spawns the Plugin (`winhome-provider-xyz.exe`) as a child process.
*   **Input (Stdin)**: The Host sends a single-line JSON object to the plugin's `StandardInput`.
*   **Output (Stdout)**: The Plugin responds by printing a single-line JSON object to `StandardOutput`.
*   **Logging (Stderr)**: The Plugin writes all logs, debug messages, and human-readable text to `StandardError`. The Host captures these and pipes them to the main application log.

## 3. The Contract (JSON Schema)

### 3.1 Request Payload (Host -> Plugin)

Sent to the plugin's `stdin`.

```json
{
  "requestId": "uuid-v4-string",
  "command": "apply" | "validate" | "metadata",
  "dryRun": true | false,
  "config": {
    // Arbitrary JSON object representing the plugin's config section
    "setting1": "value",
    "packages": ["a", "b"]
  },
  "context": {
    "osVersion": "10.0.19045",
    "isAdmin": true
  }
}
```

### 3.2 Response Payload (Plugin -> Host)

Read from the plugin's `stdout`.

```json
{
  "requestId": "uuid-v4-string", // Must match request
  "success": true,
  "changed": false,               // True if system state was modified
  "error": null,                  // Error message string if success is false
  "data": {                       // Optional result data
    "installed": ["a"],
    "skipped": ["b"]
  }
}
```

## 4. Discovery Mechanism

WinHome will scan for plugins at startup.

*   **Location**: `%LOCALAPPDATA%\WinHome\plugins`
*   **Naming Convention**:
    *   Executables: `winhome-provider-<name>.exe` (or `.bat`, `.cmd`, `.py` if associated).
    *   Manifests: `winhome-provider-<name>.yaml` (Optional, for metadata).

If the config file contains a key `docker:`, WinHome looks for `winhome-provider-docker`.

## 5. Implementation Details (C#)

### 5.1 The Interface

```csharp
public interface IPlugin
{
    string Name { get; }
    Task<PluginResult> ExecuteAsync(string command, object config, bool dryRun);
}

public record PluginResult(bool Success, bool Changed, string? Error, object? Data);
```

### 5.2 ProcessPluginRunner (Implementation)

This class wraps the complexity of process management.

```csharp
using System.Diagnostics;
using System.Text.Json;

public class ProcessPluginRunner : IPlugin
{
    private readonly string _executablePath;
    public string Name { get; }

    public ProcessPluginRunner(string name, string executablePath)
    {
        Name = name;
        _executablePath = executablePath;
    }

    public async Task<PluginResult> ExecuteAsync(string command, object config, bool dryRun)
    {
        var request = new
        {
            requestId = Guid.NewGuid().ToString(),
            command = command,
            dryRun = dryRun,
            config = config
        };

        var startInfo = new ProcessStartInfo
        {
            FileName = _executablePath,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        // Write Request
        string jsonRequest = JsonSerializer.Serialize(request);
        await process.StandardInput.WriteLineAsync(jsonRequest);
        process.StandardInput.Close(); // Signal EOF

        // Read Response
        string output = await process.StandardOutput.ReadToEndAsync();
        string error = await process.StandardError.ReadToEndAsync();
        
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            return new PluginResult(false, false, $"Plugin crashed: {error}", null);
        }

        try 
        {
            return JsonSerializer.Deserialize<PluginResult>(output);
        }
        catch
        {
            return new PluginResult(false, false, $"Invalid JSON response: {output}", null);
        }
    }
}
```

## 6. Example Usage (Python Plugin)

A simple Python script (`winhome-provider-test.py`) demonstrating the contract.

```python
import sys
import json

def main():
    # 1. Read Input
    raw_input = sys.stdin.read()
    request = json.loads(raw_input)
    
    cmd = request.get("command")
    config = request.get("config", {})
    dry_run = request.get("dryRun", False)

    # 2. Process Logic
    response = {
        "requestId": request.get("requestId"),
        "success": True,
        "changed": False,
        "error": None,
        "data": {}
    }

    if cmd == "apply":
        # Log to Stderr (Host will capture this)
        sys.stderr.write(f"Processing config: {config}\n")
        
        if not dry_run:
            # Perform action...
            response["changed"] = True
            response["data"] = {"status": "Config applied"}
        else:
             sys.stderr.write("Dry run: Skipping changes.\n")

    # 3. Write Output
    print(json.dumps(response))

if __name__ == "__main__":
    main()
```