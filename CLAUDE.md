# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

A .NET 10 CLI tool (`copilot-assets`) that distributes and syncs GitHub Copilot assets (instructions, prompts, agents, skills) to repositories. Packaged as a NuGet global tool using `System.CommandLine`.

## Build & Test Commands

```bash
# Restore dependencies
dotnet restore

# Build
dotnet build

# Run all tests
dotnet test

# Run a single test class
dotnet test --filter "FullyQualifiedName~DevTools.CopilotAssets.Tests.Services.SyncEngineTests"

# Run a single test method
dotnet test --filter "FullyQualifiedName~SyncEngineTests.SyncAsync_WithForce_OverwritesExistingFiles"

# Pack as NuGet tool
dotnet pack --configuration Release --output ./artifacts
```

Build warnings are treated as errors (`TreatWarningsAsErrors` + `EnforceCodeStyleInBuild` in Directory.Build.props).

## Architecture

**Clean architecture** with three layers:

- **Domain** (`Domain/`) — Value objects, enums, result types. `CopilotAsset`, `Manifest`, `AssetType` (Instruction, Prompt, Agent, Skill), `ValidationResult`, `DryRunResult`.
- **Services** (`Services/`) — Business logic. Key classes:
  - `PolicyAppService` (implements `IPolicyAppService`) — Main orchestrator for all commands
  - `SyncEngine` — Syncs assets from template providers to target directories, manages manifests with checksums
  - `ValidationEngine` — Validates compliance, checks checksums, scans for secrets
  - `TemplateProviderFactory` — Creates `BundledTemplateProvider` (default) or `RemoteTemplateProvider` (GitHub repos)
  - `GitHubClient` — HTTP client for GitHub API; resolves auth via `gh` CLI > `GITHUB_TOKEN` > `GH_TOKEN`
- **Commands** (`Commands/`) — CLI handlers inheriting from `BaseCommand`, which provides JSON output support and token redaction. 8 commands: init, update, validate, list, verify, config, doctor, version.
- **Infrastructure/Security** — `InputValidator`, `TokenRedactor`, `ManifestValidator`, `ContentLimits` (1 MB file limit, 100 asset max).

**DI setup** in `Program.cs` — all services registered as singletons via `Microsoft.Extensions.DependencyInjection`.

**Bundled templates** live in `templates/.github/` and are copied to output during build.

**Manifest** (`.github/.copilot-assets.json`) tracks installed assets with schema version, checksums, and source metadata.

## Test Conventions

- **xUnit** with **FluentAssertions** for assertions and **Moq** for mocking
- Global usings for `Xunit`, `FluentAssertions`, `Moq` are configured in the test .csproj
- Integration tests (`Tests/Integration/`) create isolated temp directories for end-to-end workflows
- Services are tested through their interfaces with mocked dependencies

## Code Style

- File-scoped namespaces, `var` everywhere, Allman brace style
- 4-space indentation (2 for JSON/YAML)
- LF line endings, UTF-8
- Nullable reference types enabled throughout
