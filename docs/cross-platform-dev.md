# Cross-Platform Development Guide

Although WinHome is an "Infrastructure-as-Code tool for Windows," it is built using **modern .NET 10**, allowing developers on **Linux** and **macOS** to contribute to the project effectively.

## 💻 Developing on Linux & macOS

### 1. Prerequisites
*   **[.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)**: Required for building and running unit tests.
*   **[VS Code](https://code.visualstudio.com/)**: With the **C# Dev Kit** extension.

### 2. Building the Project
You can compile the solution normally on any platform:
```bash
dotnet build WinHome.sln
```

### 3. Running Unit Tests
Our unit tests are designed with **abstractions and mocks**. This means you can run the logic-heavy unit tests (like config parsing, state management logic, and plugin discovery) without being on Windows:
```bash
dotnet test tests/WinHome.Tests/WinHome.Tests.csproj
```

---

## ⚠️ Platform Limitations

WinHome is a **Windows-native engine**. While the code compiles on other OSs, the core reconciliation logic (`Engine.cs`) contains platform guards:

*   **Registry/Services/WSL**: These modules will be skipped or throw errors if run natively on Linux/macOS.
*   **Dry-Run**: You can run `dotnet run -- --dry-run` to test configuration parsing and plugin loading, but many modules will log "Skipping: Platform not supported."

---

## 🧪 Testing Windows Logic from Linux/macOS

If you are modifying Windows-specific logic (e.g., the Registry Service) and need to verify it:

### 1. Automated CI (Recommended)
Push your branch to your fork. Our **GitHub Actions CI** (`test.yaml`) runs on `windows-latest` VMs. It will execute the full suite, including E2E tests, on a real Windows environment.

### 2. Remote Development
Use the **VS Code Remote - SSH** extension to connect to a Windows machine or VM and develop/debug directly in that environment.

### 3. Windows Containers / VM
If you have a local Windows machine, you can use the scripts in `test-data/` to run acceptance tests inside a clean Windows Sandbox.
