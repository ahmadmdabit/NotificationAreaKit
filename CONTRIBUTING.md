# Contributing to NotificationAreaKit

Thank you for your interest in contributing to NotificationAreaKit! We welcome contributions from the community to help improve this library. This document outlines the guidelines and processes for contributing.

## Table of Contents

- [Code of Conduct](#code-of-conduct)
- [Ways to Contribute](#ways-to-contribute)
- [Getting Started](#getting-started)
- [Development Setup](#development-setup)
- [Development Workflow](#development-workflow)
- [Coding Standards](#coding-standards)
- [Testing](#testing)
- [Submitting Changes](#submitting-changes)
- [Reporting Issues](#reporting-issues)
- [Community](#community)

## Code of Conduct

This project follows a code of conduct to ensure a welcoming environment for all contributors. By participating, you agree to:

- Be respectful and inclusive
- Focus on constructive feedback
- Accept responsibility for mistakes
- Show empathy towards other community members

## Ways to Contribute

You can contribute in several ways:

- **Report bugs** by opening issues
- **Suggest features** through GitHub issues
- **Improve documentation** by updating READMEs or adding examples
- **Write code** by submitting pull requests
- **Review pull requests** from other contributors
- **Help others** in discussions and issues

## Getting Started

1. **Fork the repository** on GitHub
2. **Clone your fork** locally
3. **Set up your development environment** (see below)
4. **Create a feature branch** for your changes
5. **Make your changes** following the guidelines
6. **Test your changes** thoroughly
7. **Submit a pull request**

## Development Setup

### Prerequisites

- **.NET SDK**: Version 9.0 or later
- **Windows**: 7, 10 version 19041+ or Windows 11 (for full feature testing)
- **Git**: Latest version
- **Visual Studio 2022** or **VS Code** with C# extensions (recommended)

### Clone and Setup

```bash
# Clone your fork
git clone https://github.com/your-username/NotificationAreaKit.git
cd NotificationAreaKit

# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Run the playground to verify setup
dotnet run --project NotificationAreaKit.WPF.Playground
```

### Project Structure

```
NotificationAreaKit/
├── NotificationAreaKit.Core/          # Platform-agnostic core
├── NotificationAreaKit.WPF/           # WPF-specific library
├── NotificationAreaKit.WPF.Playground/# Demo application
├── .gitignore                         # Git ignore rules
├── LICENSE.txt                        # MIT license
└── README.md                          # Project overview
```

## Development Workflow

### Branching Strategy

- `main`: Production-ready code
- `feature/*`: New features
- `bugfix/*`: Bug fixes
- `docs/*`: Documentation updates

### Commit Messages

Follow conventional commit format:

```
type(scope): description

[optional body]

[optional footer]
```

Types:
- `feat`: New features
- `fix`: Bug fixes
- `docs`: Documentation
- `style`: Code style changes
- `refactor`: Code refactoring
- `test`: Testing
- `chore`: Maintenance

Examples:
```
feat: add hover popup support
fix: resolve first-run notification failure
docs: update API reference
```

### Pull Request Process

1. **Create a branch** from `main`
2. **Make changes** with clear commit messages
3. **Test thoroughly** (see Testing section)
4. **Update documentation** if needed
5. **Create PR** with descriptive title and body
6. **Address review feedback**
7. **Merge** once approved

## Coding Standards

### C# Guidelines

- Follow [Microsoft C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Use .NET 9.0 language features (nullable reference types, etc.)
- Prefer `var` for implicit typing where clear
- Use meaningful names for variables, methods, and classes

### Code Style

```csharp
// Good: Clear naming, proper formatting
public async Task ProcessNotificationAsync(string title, string message)
{
    if (string.IsNullOrWhiteSpace(title))
    {
        throw new ArgumentException("Title cannot be null or empty", nameof(title));
    }

    // Implementation
}

// Avoid: Unclear naming, poor formatting
public async Task proc(string t, string m) {
if(string.IsNullOrEmpty(t))throw new Exception("bad");
}
```

### Project Structure

- **Internal classes**: Use `internal` visibility appropriately
- **Public API**: Keep minimal and stable
- **Separation of concerns**: Core vs WPF-specific logic
- **Documentation**: XML comments for public APIs

### WPF Specific

- Use MVVM pattern where appropriate
- Follow WPF naming conventions
- Handle UI threading properly
- Dispose resources correctly

## Testing

### Running Tests

```bash
# Run all tests
dotnet test

# Run specific project tests
dotnet test NotificationAreaKit.WPF.Tests/

# With coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Test Guidelines

- **Unit tests**: Test individual components in isolation
- **Integration tests**: Test component interactions
- **UI tests**: Test WPF-specific functionality
- **Coverage**: Aim for >80% code coverage
- **Naming**: `[MethodName]_[Scenario]_[ExpectedResult]`

### Manual Testing

- Test on multiple Windows versions (7, 10, 11)
- Verify notifications work (both Toast and Balloon)
- Test multi-icon scenarios
- Check resource cleanup (no memory leaks)

## Submitting Changes

### Pull Request Template

Use this template for PR descriptions:

```markdown
## Description
Brief description of the changes

## Type of Change
- [ ] Bug fix
- [ ] New feature
- [ ] Breaking change
- [ ] Documentation update

## Testing
- [ ] Unit tests pass
- [ ] Integration tests pass
- [ ] Manual testing completed
- [ ] Documentation updated

## Screenshots (if applicable)
Add screenshots for UI changes

## Checklist
- [ ] Code follows style guidelines
- [ ] Tests added/updated
- [ ] Documentation updated
- [ ] Breaking changes documented
```

### Review Process

- At least one maintainer review required
- CI checks must pass
- All conversations resolved
- Squash commits if requested
- Maintainers will merge approved PRs

## Reporting Issues

### Bug Reports

Include:
- **Description**: Clear steps to reproduce
- **Environment**: OS version, .NET version, library version
- **Expected vs Actual**: What should happen vs what does
- **Logs/Screenshots**: Error messages, screenshots
- **Minimal repro**: Small code sample if possible

### Feature Requests

Include:
- **Problem**: What's the issue you're solving
- **Solution**: Proposed implementation
- **Alternatives**: Other approaches considered
- **Use case**: How it would be used

### Issue Labels

- `bug`: Confirmed bugs
- `enhancement`: Feature requests
- `documentation`: Docs improvements
- `help wanted`: Good for newcomers
- `good first issue`: Simple tasks

## Community

- **Discussions**: Use GitHub Discussions for questions
- **Issues**: Bug reports and feature requests
- **Pull Requests**: Code contributions
- **Code Reviews**: Help review others' code
- **Documentation**: Improve docs and examples

### Recognition

Contributors are recognized in:
- GitHub contributor stats
- Release notes
- Project documentation

## Additional Resources

- [Project README](README.md)
- [WPF Library README](NotificationAreaKit.WPF/README.md)
- [Playground README](NotificationAreaKit.WPF.Playground/README.md)
- [.NET Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- [Conventional Commits](https://conventionalcommits.org/)

Thank you for contributing to NotificationAreaKit!