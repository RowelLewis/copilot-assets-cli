# copilot-assets

A .NET global tool for installing and managing AI coding assistant assets (prompts, agents, instructions, skills) across multiple tools and repositories. Supports **GitHub Copilot**, **Claude Code**, **Cursor**, **Windsurf**, **Cline**, and **Aider**.

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

This creates assets for your target AI tool(s). By default, assets are generated for GitHub Copilot:
- `.github/copilot-instructions.md` - Repository context and instructions
- `.github/prompts/` - Reusable prompt templates
- `.github/agents/` - AI agent definitions
- `.github/skills/` - Custom skills (SKILL.md format with YAML frontmatter)
- `.github/instructions/` - Additional custom instructions (optional folder for organizing multiple instruction files)
- `.github/.copilot-assets.json` - Manifest file tracking installed assets

### Multi-Tool Target Support

Generate assets for multiple AI coding tools from a single set of templates:

```bash
# Generate for Copilot and Claude
copilot-assets init --target copilot,claude

# Generate for all supported tools
copilot-assets init --target copilot,claude,cursor,windsurf,cline,aider

# Update with specific targets
copilot-assets update --target copilot,cursor
```

**Supported targets:** `copilot`, `claude`, `cursor`, `windsurf`, `cline`, `aider`

Each tool gets assets in its native format and directory structure:

| Tool | Instructions | Prompts | Rules/Config |
|------|-------------|---------|--------------|
| **Copilot** | `.github/copilot-instructions.md`<br/>`.github/instructions/` | `.github/prompts/` | `.github/agents/`, `.github/skills/` |
| **Claude** | `CLAUDE.md`<br/>`.claude/instructions/` | `.claude/commands/` | `.claude/skills/` |
| **Cursor** | `.cursor/rules/instructions.mdc`<br/>`.cursor/rules/` | `.cursor/rules/*.mdc` | YAML frontmatter |
| **Windsurf** | `.windsurfrules`<br/>`.windsurf/rules/` | `.windsurf/rules/` | — |
| **Cline** | `.clinerules/instructions.md`<br/>`.clinerules/` | `.clinerules/` | — |
| **Aider** | `CONVENTIONS.md`<br/>(instructions/ → root) | `.aider/prompts/` | — |

**Tool-specific content sections:** Templates can include sections for specific tools using HTML comment markers:

```markdown
# Instructions

General instructions for all tools.

<!-- copilot-only -->
Copilot-specific guidance here.
<!-- /copilot-only -->

<!-- claude-only -->
Claude-specific guidance here.
<!-- /claude-only -->
```

When generating output for a target tool, sections for other tools are automatically stripped, and the target tool's markers are removed while preserving the content.

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

### Template Pack Registry

Discover and install community template packs:

```bash
# Search for template packs
copilot-assets registry search dotnet

# List all available packs
copilot-assets registry list

# Get detailed info about a pack
copilot-assets registry info dotnet-enterprise

# Install a pack (configures it as your remote source)
copilot-assets registry install dotnet-enterprise
```

After installing a registry pack, run `copilot-assets init` or `copilot-assets update` to fetch and apply the templates.

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

### Path-Specific Custom Instructions

The `.github/instructions/` folder supports organizing multiple instruction files for different parts of your codebase. Each instruction file can include YAML frontmatter with glob patterns to specify which files they apply to.

**Example:** `coding-standards.md`
```yaml
---
applyTo: "**/*.cs,**/*.ts,**/*.js,**/*.py"
---

# Coding Standards

Follow these coding standards when working on this project:
- Use clear, descriptive variable and function names
- Keep functions small and focused
- Add comments for complex logic
```

**Interactive Mode Display:**
- Instructions are grouped by file (e.g., "Coding Standards Instructions", "Security Practices Instructions")
- Each instruction file can be selected individually
- Displayed with friendly title-cased names derived from filenames

**Supported by:**
- GitHub Copilot (`.github/instructions/`)
- Claude Code (`.claude/instructions/`)
- Cursor (`.cursor/rules/*.mdc`)

When using `--target` with multiple tools, instruction files are adapted to each tool's native format while preserving the frontmatter and content.

### Update to Latest Version

```bash
# Update the tool itself
dotnet tool update -g copilot-assets

# Then update assets in your project
cd your-project
copilot-assets update
```

### Fleet Management

Manage assets across multiple repositories from a single configuration:

```bash
# Add repositories to your fleet
copilot-assets fleet add org/repo-1
copilot-assets fleet add org/repo-2 --source my-templates --target copilot,claude

# List all fleet repositories
copilot-assets fleet list

# Check sync status across all repos
copilot-assets fleet status

# Validate compliance across the fleet
copilot-assets fleet validate

# Remove a repository from the fleet
copilot-assets fleet remove org/repo-1
```

Fleet configuration supports per-repo overrides for source, targets, and branch, with fleet-wide defaults:

```json
{
  "version": 1,
  "repos": [
    { "name": "org/repo-1", "source": "my-templates", "targets": ["copilot", "claude"] },
    { "name": "org/repo-2", "branch": "develop" }
  ],
  "defaults": {
    "source": "default",
    "targets": ["copilot"],
    "branch": "main"
  }
}
```

Fleet config is stored at `~/.config/copilot-assets/fleet.json`.

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

Validation includes:
- Required file checks
- SKILL.md frontmatter validation (must have `name` field and non-empty body)
- Secret pattern detection (API keys, tokens, private keys)

### JSON Output

All commands support `--json` for machine-readable output:

```bash
copilot-assets init --json
copilot-assets update --json
copilot-assets list --json
copilot-assets verify --json
copilot-assets validate --json
copilot-assets fleet list --json
copilot-assets registry search dotnet --json
```

### Diagnose Environment

```bash
copilot-assets doctor
```

## Commands

```
copilot-assets init [options]       Initialize assets for target AI tools
copilot-assets update [options]     Update assets to latest version
copilot-assets config <command>     Manage remote template configuration
copilot-assets registry <command>   Discover and install community template packs
copilot-assets fleet <command>      Manage assets across multiple repositories
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

### Registry Subcommands

```
copilot-assets registry search <query>  Search for template packs
copilot-assets registry list            List all available packs
copilot-assets registry info <name>     Show detailed pack information
copilot-assets registry install <name>  Install a pack as remote source
```

### Fleet Subcommands

```
copilot-assets fleet add <repo>         Add a repository to the fleet
copilot-assets fleet remove <repo>      Remove a repository from the fleet
copilot-assets fleet list               List all fleet repositories
copilot-assets fleet validate           Validate compliance across the fleet
copilot-assets fleet status             Show sync status for all fleet repos
```

### Global Options

- `--json` - Output results as JSON (all commands)

### Command Options

#### `init`
- `<path>` - Target directory (default: current directory)
- `-i, --interactive` - Review and select files individually or by folder
- `-f, --force` - Overwrite existing files
- `-s, --source <source>` - Template source: 'default' or 'owner/repo[@branch]'
- `-t, --target <targets>` - Target AI tools (comma-separated): copilot, claude, cursor, windsurf, cline, aider
- `--no-git` - Skip Git operations (staging/commits)
- `--only <types>` - Install only specified types (instruction, prompts, agents, skills)
- `--exclude <types>` - Exclude specified types
- `--dry-run` - Preview changes without making modifications

#### `update`
- `<path>` - Target directory (default: current directory)
- `-i, --interactive` - Review and select files individually or by folder
- `-f, --force` - Force update even if already at latest version
- `-s, --source <source>` - Template source: 'default' or 'owner/repo[@branch]'
- `-t, --target <targets>` - Target AI tools (comma-separated)
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

#### `registry`
- `search <query>` - Search packs by name, description, or tags
- `list` - List all available packs
- `info <name>` - Show detailed pack information
- `install <name>` - Configure a pack as the remote template source

#### `fleet`
- `add <repo>` - Add repository (owner/repo format)
  - `--source <source>` - Template source for this repo
  - `--target <targets>` - Target tools (comma-separated)
  - `--branch <branch>` - Branch to target
- `remove <repo>` - Remove repository from fleet
- `list` - List all fleet repositories with effective settings
- `validate` - Validate compliance across all fleet repos
- `status` - Show sync status for all fleet repos

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

**Registry Packs** (community)
- Discoverable via `copilot-assets registry search`
- Install with a single command
- Curated template collections with multi-tool support

**Template Resolution:**
- Interactive mode (`-i`): Prompts for source selection
- Command-line (`--source`): Uses specified source
- Configured remote: Uses saved configuration
- Default: Uses bundled templates

## Installed Asset Structure

Default output for Copilot:

```
.github/
├── copilot-instructions.md          # Main instructions file
├── .copilot-assets.json             # Manifest (auto-generated)
├── prompts/
│   ├── code-review.prompt.md
│   ├── generate-docs.prompt.md
│   └── generate-tests.prompt.md
├── agents/
│   ├── documenter.agent.md
│   └── reviewer.agent.md
├── skills/
│   └── refactor/
│       └── SKILL.md                 # YAML frontmatter + markdown body
└── instructions/
    ├── coding-standards.md          # Custom instructions (optional folder)
    └── security-practices.md
```

When targeting multiple tools (e.g., `--target copilot,claude,cursor`), additional output is generated:

```
CLAUDE.md                            # Claude Code instructions
CONVENTIONS.md                       # Aider conventions
.windsurfrules                       # Windsurf instructions
.claude/
├── commands/                        # Claude prompts & agents
└── skills/
    └── refactor/
        └── SKILL.md
.cursor/
└── rules/
    ├── instructions.mdc             # Cursor rules with YAML frontmatter
    └── code-review.mdc
.clinerules/
├── instructions.md
└── code-review.prompt.md
.aider/
└── prompts/
    └── code-review.prompt.md
.windsurf/
└── rules/
    └── code-review.prompt.md
```

## Security

- **Input validation** - Repository names, branch names, and file paths are validated to prevent injection and path traversal attacks
- **Token redaction** - Sensitive tokens (GitHub PATs, Bearer tokens, API keys) are redacted from error messages and logs
- **Manifest validation** - Manifests are checked for excessive asset counts, path traversal, and valid checksums
- **Content limits** - Files are limited to 1 MB and manifests to 100 assets to prevent DoS

See [SECURITY.md](SECURITY.md) for the full security policy.

## Uninstall

```bash
dotnet tool uninstall -g copilot-assets
```

## License

MIT License - see [LICENSE](LICENSE) file for details
