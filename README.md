# copilot-assets

A .NET global tool for installing and managing GitHub Copilot assets (prompts, agents, instructions, skills) in your projects.

## Installation

### From Azure DevOps Artifacts (Private Feed)

```bash
# Add your private feed (one-time setup)
dotnet nuget add source "https://pkgs.dev.azure.com/{org}/{project}/_packaging/{feed}/nuget/v3/index.json" \
  --name "MyOrgFeed" \
  --username "AzureDevOps" \
  --password "{PAT_TOKEN}"

# Install the tool globally
dotnet tool install -g copilot-assets --add-source MyOrgFeed
```

### From NuGet.org (Public)

```bash
dotnet tool install -g copilot-assets
```

## Usage

### Initialize Assets in a Project

```bash
cd your-project
copilot-assets init
```

This creates:
- `.github/copilot-instructions.md` - Repository context and instructions
- `.github/prompts/` - Reusable prompt templates
- `.github/agents/` - AI agent definitions
- `.github/skills/` - Custom Copilot skills
- `.github/.copilot-assets.json` - Manifest file tracking installed version

### Update to Latest Version

```bash
# Update the tool itself
dotnet tool update -g copilot-assets

# Then update assets in your project
cd your-project
copilot-assets update
```

### Validate Asset Compliance

```bash
# Development mode
copilot-assets validate

# CI/CD mode (strict validation, JSON output)
copilot-assets validate --ci
```

### Diagnose Environment

```bash
copilot-assets doctor
```

## Commands

```
copilot-assets init [options]       Initialize Copilot assets
copilot-assets update [options]     Update assets to latest version
copilot-assets validate [options]   Validate asset compliance
copilot-assets doctor               Check environment and diagnose issues
copilot-assets version              Display version information
```

### Command Options

#### `init`
- `-d, --directory <path>` - Target directory (default: current directory)
- `-f, --force` - Overwrite existing files
- `--no-git` - Skip Git operations (staging/commits)

#### `update`
- `-d, --directory <path>` - Target directory (default: current directory)
- `-f, --force` - Force update even if already at latest version
- `--no-git` - Skip Git operations

#### `validate`
- `-d, --directory <path>` - Target directory (default: current directory)
- `--ci` - CI mode with strict validation and JSON output

## Installed Asset Structure

```
.github/
├── copilot-instructions.md          # Main instructions file
├── .copilot-assets.json             # Version manifest (auto-generated)
├── prompts/
│   ├── code-review.prompt.md
│   ├── generate-docs.prompt.md
│   └── generate-tests.prompt.md
├── agents/
│   ├── documenter.agent.md
│   └── reviewer.agent.md
└── skills/
    ├── analyze-file.skill.md
    └── refactor.skill.md
```

## Uninstall

```bash
dotnet tool uninstall -g copilot-assets
```

## License

MIT License - see [LICENSE](LICENSE) file for details
