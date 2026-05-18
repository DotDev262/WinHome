# WinHome PowerShell Plugin Implementation
# Implements the process-based JSON-over-Stdin/Stdout IPC protocol.

# 1. Read input from standard input (stdin)
$jsonInput = [System.Console]::In.ReadToEnd()
if ([string]::IsNullOrWhiteSpace($jsonInput)) {
    $jsonInput = $Input | Out-String
}

# 2. Parse request JSON
try {
    $request = ConvertFrom-Json $jsonInput
} catch {
    [System.Console]::Error.WriteLine("[test-powershell] Error: Failed to parse request JSON: $_")
    exit 1
}

$command = $request.command
$requestId = $request.requestId
$args = $request.args
$context = $request.context

# Log info to stderr (Host will capture and pipe to WinHome main logs)
[System.Console]::Error.WriteLine("[test-powershell] Received command '$command' with requestId '$requestId'")

# 3. Setup Response Structure
$success = $true
$changed = $false
$err = $null
$data = $null

# 4. Command Routing Logic
switch ($command) {
    "check_installed" {
        $packageId = $args.packageId
        [System.Console]::Error.WriteLine("[test-powershell] Checking if $packageId is installed...")
        # Simulated check: suppose 'demo-pkg' is installed, others not.
        if ($packageId -eq "demo-pkg") {
            $data = $true
        } else {
            $data = $false
        }
    }

    "install" {
        $packageId = $args.packageId
        [System.Console]::Error.WriteLine("[test-powershell] Installing $packageId...")
        $changed = $true
        $data = "Installed package $packageId successfully."
    }

    "uninstall" {
        $packageId = $args.packageId
        [System.Console]::Error.WriteLine("[test-powershell] Uninstalling $packageId...")
        $changed = $true
        $data = "Uninstalled package $packageId successfully."
    }

    "apply" {
        [System.Console]::Error.WriteLine("[test-powershell] Applying configuration...")
        if ($null -ne $args) {
            foreach ($key in $args.psobject.properties.name) {
                $val = $args.$key
                [System.Console]::Error.WriteLine("[test-powershell] Setting $key = $val")
            }
        }
        $changed = $true
        $data = "Configuration applied successfully."
    }

    default {
        $success = $false
        $err = "Unknown command: $command"
    }
}

# 5. Output Response JSON to stdout
$response = @{
    requestId = $requestId
    success   = $success
    changed   = $changed
    error     = $err
    data      = $data
}

$jsonResponse = $response | ConvertTo-Json -Compress
Write-Output $jsonResponse
