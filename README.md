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

### Remote Template Configuration

Configure a custom GitHub repository as your template source:

```bash
# Set a remote template source
copilot-assets config set source owner/repo

# Set a specific branch (default: main)
copilot-assets config set branch develop

# View current configuration
copilot-assets config list

# Get a specific config value
copilot-assets config get source

# Reset to bundled templates
copilot-assets config reset
```

When configured, `init` and `update` commands will:
1. Fetch templates from your GitHub repository
2. Fall back to cached version if network fails
3. Use bundled templates if no cache exists

**Authentication:** Supports private repositories via:
- `gh` CLI (recommended): `gh auth login`
- Environment variables: `GITHUB_TOKEN` or `GH_TOKEN`

### Selective Installation

```bash
# Install only prompts and agents
copilot-assets init --only prompts,agents

# Install everything except skills
copilot-assets init --exclude skills

# Just the instructions file (minimal)
copilot-assets init --only instruction
```

### Interactive Mode

Review and select files individually or by folder using arrow-key navigation:

```bash
# Interactive installation - review each file/folder
copilot-assets init -i

# Interactive update - review only changed files
copilot-assets update -i
```

**Features:**
- **Source Selection** - Choose between default templates or remote repository with ←/→ arrows
- **Folder Actions** - For each folder, choose to add all, review one-by-one, or skip
- **File Selection** - For individual files, choose install/update or skip
- **Visual Feedback** - Color-coded status indicators (new files in green, modified in yellow)
- **Summary** - See total files selected and skipped before commit

**Example workflow:**
```
Select template source:
 → Default templates (included with tool)    Remote repository (GitHub)...

Using default templates

  copilot-instructions.md (new)
   Install   Skip 
  ✓ Added

  prompts/ (3 files, 3 new)
   Add all   Review one-by-one   Skip folder 
  ✓ Added 3 files

Summary: 4 to install, 0 skipped
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

# Filter by type
copilot-assets list --type prompts
```

### Verify Asset Integrity

```bash
# Check if any files have been modified
copilot-assets verify

# Restore modified files to original state
copilot-assets verify --restore

# Verify only specific type
copilot-assets verify --only prompts
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
copilot-assets init --json
copilot-assets update --json
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
copilot-assets config <command>     Manage remote template configuration
copilot-assets list [options]       List installed assets
copilot-assets verify [options]     Verify file integrity
copilot-assets validate [options]   Validate asset compliance
copilot-assets doctor               Check environment and diagnose issues
copilot-assets version              Display version information
```

### Config Subcommands

```
copilot-assets config get <key>         Get configuration value (source, branch)
copilot-assets config set <key> <value> Set configuration value
copilot-assets config list              List all configuration
copilot-assets config reset             Reset to bundled templates
```

### Global Options

- `--json` - Output results as JSON (all commands)

### Command Options

#### `init`
- `<path>` - Target directory (default: current directory)
- `-i, --interactive` - Review and select files individually or by folder
- `-f, --force` - Overwrite existing files
- `-s, --source <source>` - Template source: 'default' or 'owner/repo[@branch]'
- `--no-git` - Skip Git operations (staging/commits)
- `--only <types>` - Install only specified types (instruction, prompts, agents, skills)
- `--exclude <types>` - Exclude specified types
- `--dry-run` - Preview changes without making modifications

#### `update`
- `<path>` - Target directory (default: current directory)
- `-i, --interactive` - Review and select files individually or by folder
- `-f, --force` - Force update even if already at latest version
- `-s, --source <source>` - Template source: 'default' or 'owner/repo[@branch]'
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

#### `config`
- `get <key>` - Get configuration value (keys: source, branch)
- `set <key> <value>` - Set configuration value
- `list` - Show all configuration settings
- `reset` - Reset to default bundled templates

## Template Sources

**Default Templates** (bundled)
- Included with the tool
- Always available offline
- Updated with tool releases

**Remote Templates** (configurable)
- Fetch from any GitHub repository
- Interactive source selection with arrow keys
- Supports custom organizational templates
- Supports private repositories with authentication
- Can override per-command with `--source` flag

**Template Resolution:**
- Interactive mode (`-i`): Prompts for source selection
- Command-line (`--source`): Uses specified source
- Configured remote: Uses saved configuration
- Default: Uses bundled templates

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
