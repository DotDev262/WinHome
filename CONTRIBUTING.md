# Contributing to WinHome

First off, thank you for considering contributing to WinHome! It's people like you that make WinHome such a great tool.

## 🌈 GSSOC 2026 Participants
Welcome! We are excited to have you. To ensure your contribution is tracked correctly on the leaderboard, please note:

### 🏷️ Mandatory PR Labels
Maintainers will apply these labels to your Pull Requests. They are required for GSSOC tracking:
- **gssoc:approved**: Every PR **must** have this label to be counted for the program.
- **Difficulty**: One of `level:beginner`, `level:intermediate`, `level:advanced`, or `level:critical`.
- **Type**: Labels like `type:bug`, `type:feature`, `type:docs`, `type:testing`, etc.
- **Quality**: Optional multipliers like `quality:clean` or `quality:exceptional`.

## 🚀 Getting Started

If you've noticed a bug or have a feature request, [open an issue](https://github.com/DotDev262/WinHome/issues/new/choose)! Please wait for approval before starting work on a major feature.

### Fork & Create a Branch
1. [Fork WinHome](https://github.com/DotDev262/WinHome/fork).
2. Create a branch with a descriptive name: `git checkout -b 38-add-awesome-new-feature` (where 38 is the issue number).

### Build & Test
- **Build**: `dotnet build WinHome.sln`
- **Test**: `dotnet test WinHome.sln`
- **Linux/macOS**: See the [Cross-Platform Development Guide](./docs/cross-platform-dev.md) for details on using our mock-based testing suite.

## 🗺️ Codebase Map
Understanding the directory structure:
- **`src/`**: The core application source code.
  - **`Engine.cs`**: The main reconciliation engine.
  - **`Services/`**: Business logic for Windows, WSL, Registry, and Package Managers.
  - **`Models/`**: C# classes representing `config.yaml` structures.
  - **`Infrastructure/`**: CLI parsing and application host logic.
- **`tests/`**: Unit and integration tests.
  - **`WinHome.Tests/`**: The main xUnit test suite.
- **`plugins/`**: Source code for built-in Python and JavaScript plugins.
- **`docs/`**: Documentation files and module guides.
- **`.github/`**: Workflows, issue templates, and pull request templates.

## 📜 Code Style & Standards

### Formatting
We use standard .NET coding conventions. Before submitting, please run:
```bash
dotnet format WinHome.sln
```
Our CI/CD pipeline will verify that the code is formatted correctly.

### Commit Messages
We follow a structured format for commit messages: `<Type>: <Brief description>`
- `Fix`: Bug fixes.
- `Feat`: New features.
- `Docs`: Documentation changes.
- `Chore`: Maintenance, CI, or dependency updates.
- `Test`: Adding or improving tests.

Example: `Feat: Add support for transparency effects in SystemSettings`

## 🧪 Test Expectations
- **No Regressions**: Existing tests must pass.
- **New Features**: Every new feature or bug fix must include corresponding unit tests in the `tests/WinHome.Tests/` directory.
- **Mocks**: When working on Windows-specific services, use the `Moq` framework to ensure tests remain runnable on Linux/macOS.


### Plugin Tests
If you add or modify a plugin in `plugins/`, ensure it has a test file:
- **Python plugins**: `plugins/<name>/test/test_<name>.py` with `if __name__ == "__main__":` at the bottom
- **JS/TS plugins**: a `test` script defined in `package.json`

Plugin tests run automatically via the **Test Plugins** CI workflow on every push and PR to `main`.


## 📝 Pull Request Guidelines
- **Template**: Fill out the PR template completely.
- **Issue Link**: Always link the PR to the relevant GitHub issue (e.g., `Closes #38`).
- **Single Focus**: Keep PRs focused on a single change. Large, multi-purpose PRs will be asked to be split.

## ⏱️ Review Timeline
We value your time!
- **Initial Triage**: Issues and PRs are usually triaged within **24 hours**.
- **Full Review**: Expect a detailed code review within **48-72 hours**.
- If you don't hear from us after 3 days, feel free to @mention a maintainer in the comments.

## 💬 Contact
For quick questions or community support, please use our **[GitHub Discussions](https://github.com/DotDev262/WinHome/discussions)**.

Thank you for your contribution!
If you encounter issues while setting up, see our [Troubleshooting Guide](docs/troubleshooting.md).
