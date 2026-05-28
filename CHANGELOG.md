# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Centralized plugin directory page in documentation.
- AutoHotkey config provider plugin.
- Opencode config provider plugin.
- Pip package manager configuration plugin.
- `.editorconfig` for consistent coding styles.

### Fixed
- Addressed code review comments for the pip plugin.
- Pip plugin: Return bare bool in `check_installed` and add corrupted config recovery test.

### Changed
- Enforce LF line endings across the repository.
- Update test arguments to match settings protocol.

### Dependencies
- Bumped `Microsoft.NET.Test.Sdk` from 18.5.1 to 18.6.0.
