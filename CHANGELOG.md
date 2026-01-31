# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2026-01-31

### Added
- Initial release of copilot-assets CLI tool
- `init` command to initialize GitHub Copilot assets in projects
- `update` command to sync assets to latest version
- `validate` command for asset compliance checking (with `--ci` mode)
- `doctor` command for environment diagnostics
- `version` command to display tool version
- Template assets: copilot-instructions.md, prompts, agents, and skills
- Automatic .gitignore management to ensure assets are tracked
- Manifest file (.copilot-assets.json) for version tracking
- Git integration (automatic staging and commits)
- Clean Architecture implementation with dependency injection
- LibGit2Sharp integration for Git operations
- Comprehensive test suite (111 unit and integration tests)
- Azure DevOps CI/CD pipelines for build and enforcement
- Command options: `--force`, `--no-git`, `--directory`
