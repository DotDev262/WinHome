# Testing Guide

This document outlines the testing strategy for WinHome. Because WinHome modifies system state (registry, packages, environment variables), testing requires strict isolation to prevent damaging the host machine and to ensure idempotency.

## Testing Tiers

We employ a three-tier testing strategy:

1.  **Unit Tests (Fast)**: Validate logic, parsers, and service abstractions in isolation.
2.  **Container Integration (CI)**: Validates package manager interactions in a clean Windows Server Core environment.
3.  **Sandbox / VM (Full System)**: Validates deep OS integrations (Service Control Manager, Registry Hives) that may differ or fail in containers.

---

## Local Development Workflow

### 1. Unit Tests
Run standard .NET unit tests locally. These have no system side effects.

```powershell
dotnet test tests/WinHome.Tests/
```

### 2. Windows Sandbox ("Clean Room" Testing)
For rapid integration testing, we use **Windows Sandbox**. This provides a temporary, disposable Windows desktop environment that starts in seconds.

**Prerequisites:**
- Windows 10/11 Pro or Enterprise
- "Windows Sandbox" feature enabled

**Full System Integration Test:**
1.  Execute the launcher script:
    ```powershell
    powershell -File testing/infrastructure/start-sandbox.ps1
    ```
2.  The sandbox will automatically build the project, setup plugins, install runtimes (`uv`, `bun`), and run the full test configuration (`test-config.yaml`).
3.  Verification results will be displayed in the terminal upon logon.

**Plugin-Only Integration Test:**
For faster iteration when working on plugins:
1.  Ensure the project is built (`dotnet publish`).
2.  Launch the specialized sandbox:
    ```powershell
    Invoke-Item testing/infrastructure/WinHome-Plugins.wsb
    ```

**Why use this?**
- **Safety**: Changes (installing apps, changing registry) are discarded when you close the window.
- **Idempotency**: Every run starts from a pristine Windows state, ensuring your configuration works on fresh systems.

---

## Automated CI (Docker)

Our GitHub Actions pipeline utilizes **Windows Server Core** containers to run integration tests.

### Dockerfile Strategy
- **Location**: `testing/infrastructure/Dockerfile`.
- **Base Image**: `mcr.microsoft.com/windows/servercore:ltsc2022`.
- **Optimizations**: 
    - Layer caching for `.csproj` restores.
    - Pester module pre-installed.
    - Full plugin source code and runtimes included.

### Running Container Tests Locally
If you have Docker Desktop for Windows configured for **Windows Containers**:

```powershell
# Build and Run from project root
docker build -t winhome:test -f testing/infrastructure/Dockerfile .
docker run winhome:test
```

This executes the `test-data/run-test-container.ps1` script, which:
1.  Installs a test configuration.
2.  Runs WinHome.
3.  Executes the Pester verification suite.

---

## Verification Framework (Pester)

We use [Pester](https://pester.dev/) for structured integration assertions. The test suite is located at `test-data/verify.Tests.ps1`.

### Key Scenarios Covered
- **Package Managers**: Verifies installation of packages via Scoop, Chocolatey, and Winget.
- **Environment**: Checks for correct environment variable persistence.
- **System Settings**: Validates Registry keys and Windows Settings (e.g., showing file extensions).
- **Dotfiles**: Ensures configuration files are correctly linked.

To run these tests manually against your local machine (**Use with caution**):
```powershell
Invoke-Pester -Path test-data/verify.Tests.ps1 -Output Detailed
```

---

## Release Validation (Virtual Machines)

For final release candidates, we recommend testing on a full Virtual Machine (Hyper-V / VMware) to validate behaviors that cannot be simulated in containers (e.g., reboots, specific driver interactions).

**Recommended Workflow:**
1.  **Snapshot**: Always take a snapshot of a clean VM state.
2.  **Execute**: Run the release binary.
3.  **Verify**: Check all system states.
4.  **Revert**: Revert to the snapshot for the next test run.

*Future Note: We aim to automate this tier using GitHub Actions Self-Hosted Runners.*
