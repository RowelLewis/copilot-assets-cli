# copilot-assets

A .NET global tool for installing and managing GitHub Copilot assets (prompts, agents, instructions, skills) in your projects.

## Installation

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

### Selective Installation

```bash
# Install only prompts and agents
copilot-assets init --only prompts,agents

# Install everything except skills
copilot-assets init --exclude skills

# Just the instructions file (minimal)
copilot-assets init --only instruction
```

### Preview Changes (Dry Run)

```bash
copilot-assets init --dry-run
```

### Update to Latest Version

```bash
# Update the tool itself
dotnet tool update -g copilot-assets

# Then update assets in your project
cd your-project
copilot-assets update
```

### List Installed Assets

```bash
copilot-assets list
```

### Verify Asset Integrity

```bash
# Check if any files have been modified
copilot-assets verify

# Restore modified files to original state
copilot-assets verify --restore
```

### Validate Asset Compliance

```bash
# Development mode
copilot-assets validate

# CI/CD mode (strict validation, JSON output)
copilot-assets validate --ci
```

### JSON Output

All commands support `--json` for machine-readable output:

```bash
copilot-assets list --json
copilot-assets verify --json
copilot-assets validate --json
```

### Diagnose Environment

```bash
copilot-assets doctor
```

## Commands

```
copilot-assets init [options]       Initialize Copilot assets
copilot-assets update [options]     Update assets to latest version
copilot-assets list [options]       List installed assets
copilot-assets verify [options]     Verify file integrity
copilot-assets validate [options]   Validate asset compliance
copilot-assets doctor               Check environment and diagnose issues
copilot-assets version              Display version information
```

### Global Options

- `--json` - Output results as JSON (all commands)

### Command Options

#### `init`
- `<path>` - Target directory (default: current directory)
- `-f, --force` - Overwrite existing files
- `--no-git` - Skip Git operations (staging/commits)
- `--only <types>` - Install only specified types (instruction, prompts, agents, skills)
- `--exclude <types>` - Exclude specified types
- `--dry-run` - Preview changes without making modifications

#### `update`
- `<path>` - Target directory (default: current directory)
- `-f, --force` - Force update even if already at latest version
- `--no-git` - Skip Git operations
- `--only <types>` - Update only specified types
- `--exclude <types>` - Exclude specified types
- `--dry-run` - Preview changes without making modifications

#### `list`
- `<path>` - Target directory (default: current directory)
- `--type <type>` - Filter by asset type

#### `verify`
- `<path>` - Target directory (default: current directory)
- `--restore` - Restore modified files to original state
- `--only <type>` - Verify only specified type

#### `validate`
- `<path>` - Target directory (default: current directory)
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
