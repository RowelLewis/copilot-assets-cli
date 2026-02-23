# Security Policy

## Reporting a Vulnerability

If you discover a security vulnerability in copilot-assets-cli, please report it responsibly:

- **Email:** security@[your-domain].com (or create a private security advisory on GitHub)
- **Response Time:** We aim to respond within 48 hours
- **Disclosure:** We will coordinate disclosure timing with you

Please include:
- Description of the vulnerability
- Steps to reproduce
- Affected versions
- Any proof-of-concept code (if applicable)

**Do not** open public GitHub issues for security vulnerabilities.

## Security Best Practices

### Authentication

1. **Use `gh` CLI for token management:**
   ```bash
   gh auth login
   ```
   This stores your GitHub token securely in your OS credential manager.

2. **Alternative: Environment variables:**
   ```bash
   export GITHUB_TOKEN=your_token_here
   ```
   Note: Environment variables are less secure than `gh` CLI.

3. **Never commit tokens** to git repositories or share them in public channels.

### Remote Templates

1. **Only configure trusted repositories:**
   ```bash
   copilot-assets config set source trusted-org/trusted-repo
   ```

2. **Review remote templates before installation:**
   - Check the repository contents on GitHub
   - Verify the repository owner is trustworthy
   - Use the `--dry-run` flag to preview changes

3. **Use specific branches for stability:**
   ```bash
   copilot-assets config set branch production
   ```

### Updates

Keep the tool updated to receive security fixes:

```bash
dotnet tool update --global copilot-assets
```

Check for updates monthly or when notified of security advisories.

## Security Features

copilot-assets-cli includes several security features:

- **Token Redaction:** GitHub tokens are automatically redacted from error messages and logs
- **Path Traversal Prevention:** File paths are validated to prevent directory traversal attacks
- **File Size Limits:** Files larger than 1 MB are rejected to prevent DoS attacks
- **Asset Count Limits:** Maximum of 100 assets per manifest to prevent resource exhaustion
- **Input Validation:** Repository names and branch names are strictly validated
- **Secret Pattern Detection:** Built-in patterns detect common secrets in asset files
- **HTTPS Only:** All communication with GitHub uses encrypted HTTPS
- **Dependency Scanning:** NuGet packages are automatically scanned for vulnerabilities

## Known Limitations

- **Remote Content Trust:** The tool trusts all content from configured GitHub repositories. Only configure repositories you control or trust.
- **Local File Access:** The tool can write to your `.github` directory. Do not run it in untrusted directories.
- **GitHub API Rate Limits:** Without authentication, GitHub limits you to 60 API calls per hour.

## Supported Versions

| Version | Supported          |
| ------- | ------------------ |
| 1.3.x   | :white_check_mark: |
| 1.2.x   | :white_check_mark: |
| 1.1.x   | :x: (upgrade recommended) |
| 1.0.x   | :x: (upgrade required) |

## Security Update Policy

- **Critical vulnerabilities:** Patched within 7 days
- **High severity:** Patched within 30 days
- **Medium severity:** Patched in next minor release
- **Low severity:** Addressed in regular updates

## Vulnerability Disclosure Timeline

1. **Day 0:** Vulnerability reported privately
2. **Day 1-2:** Acknowledge receipt and begin investigation
3. **Day 3-7:** Develop and test fix
4. **Day 8-14:** Release patched version
5. **Day 14+:** Public disclosure after users have time to update

## Contact

For security concerns, contact:
- **Security Team:** security@[your-domain].com
- **GitHub Security Advisories:** https://github.com/RowelLewis/copilot-assets-cli/security/advisories

Thank you for helping keep copilot-assets-cli secure!
