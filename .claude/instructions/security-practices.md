---
applyTo: "**"
---

# Security Practices

Always follow these security practices:

## Input Validation
- Validate all user inputs
- Sanitize data before processing
- Use parameterized queries for database operations
- Never trust client-side validation alone

## Authentication & Authorization
- Use strong authentication mechanisms
- Implement proper session management
- Follow the principle of least privilege
- Never hardcode credentials

## Data Protection
- Encrypt sensitive data at rest and in transit
- Use secure hash algorithms for passwords
- Implement proper key management
- Regular security audits

## Error Handling
- Never expose stack traces to users
- Log errors securely without sensitive data
- Fail securely with appropriate error messages
