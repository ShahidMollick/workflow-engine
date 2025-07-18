# Contributing to Configurable Workflow Engine

Thank you for your interest in contributing to the Configurable Workflow Engine! This document provides guidelines and instructions for contributing to the project.

## 🎯 Code of Conduct

By participating in this project, you agree to abide by our [Code of Conduct](CODE_OF_CONDUCT.md). Please read it before contributing.

## 🚀 Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later
- Git for version control
- IDE: Visual Studio 2022, VS Code, or JetBrains Rider

### Development Setup

1. **Fork the repository**
   ```bash
   git clone https://github.com/YOUR-USERNAME/workflow-engine.git
   cd workflow-engine
   ```

2. **Set up development environment**
   ```bash
   # Restore dependencies
   dotnet restore
   
   # Build the solution
   dotnet build
   
   # Run tests
   dotnet test
   
   # Start the API (optional)
   cd src/Infonetica.WorkflowEngine.Api
   dotnet run
   ```

3. **Verify setup**
   - All tests should pass
   - Application should start without errors
   - API should be accessible at `http://localhost:5004`

## 📋 How to Contribute

### Types of Contributions

We welcome various types of contributions:

- 🐛 **Bug Reports**: Help us identify and fix issues
- ✨ **Feature Requests**: Suggest new functionality
- 📝 **Documentation**: Improve or add documentation
- 🔧 **Code Contributions**: Bug fixes, features, performance improvements
- 🧪 **Testing**: Add test coverage or improve existing tests

### Before You Start

1. **Check existing issues** to avoid duplicate work
2. **Create an issue** for significant changes to discuss the approach
3. **Follow the coding standards** outlined below
4. **Write tests** for new functionality

## 🔧 Development Workflow

### Branch Naming Convention

```
feature/feature-name     # New features
bugfix/issue-description # Bug fixes
hotfix/critical-fix      # Critical production fixes
docs/documentation-update # Documentation changes
test/test-improvements   # Test-related changes
```

### Commit Message Format

Follow the [Conventional Commits](https://www.conventionalcommits.org/) specification:

```
<type>[optional scope]: <description>

[optional body]

[optional footer(s)]
```

**Types:**
- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation changes
- `style`: Formatting, missing semicolons, etc.
- `refactor`: Code refactoring
- `test`: Adding or updating tests
- `chore`: Maintenance tasks

**Examples:**
```
feat(api): add workflow versioning support
fix(validation): handle null state transitions correctly
docs(readme): update installation instructions
test(service): add integration tests for workflow execution
```

### Pull Request Process

1. **Create a feature branch**
   ```bash
   git checkout -b feature/your-feature-name
   ```

2. **Make your changes**
   - Follow coding standards
   - Add tests for new functionality
   - Update documentation if needed

3. **Test your changes**
   ```bash
   dotnet test
   dotnet build --configuration Release
   ```

4. **Commit your changes**
   ```bash
   git add .
   git commit -m "feat(api): add your feature description"
   ```

5. **Push to your fork**
   ```bash
   git push origin feature/your-feature-name
   ```

6. **Create a Pull Request**
   - Use the PR template
   - Link related issues
   - Describe changes clearly
   - Add screenshots if applicable

## 📝 Coding Standards

### C# Style Guide

Follow [Microsoft C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/inside-a-program/coding-conventions):

```csharp
// ✅ Good: PascalCase for public members
public class WorkflowService
{
    // ✅ Good: camelCase for private fields with underscore prefix
    private readonly IWorkflowRepository _repository;
    
    // ✅ Good: Meaningful names
    public async Task<WorkflowDefinition> CreateWorkflowDefinitionAsync(CreateWorkflowRequest request)
    {
        // ✅ Good: Guard clauses early
        if (request == null)
            throw new ArgumentNullException(nameof(request));
        
        // ✅ Good: Single responsibility
        ValidateWorkflowRequest(request);
        var definition = MapToWorkflowDefinition(request);
        await _repository.SaveDefinitionAsync(definition);
        
        return definition;
    }
}
```

### Code Quality Rules

1. **SOLID Principles**
   - Single Responsibility Principle
   - Open/Closed Principle
   - Liskov Substitution Principle
   - Interface Segregation Principle
   - Dependency Inversion Principle

2. **Naming Conventions**
   ```csharp
   // Classes: PascalCase
   public class WorkflowEngine { }
   
   // Methods: PascalCase
   public void ExecuteAction() { }
   
   // Properties: PascalCase
   public string WorkflowId { get; set; }
   
   // Fields: camelCase with underscore
   private readonly ILogger _logger;
   
   // Constants: PascalCase
   public const string DefaultState = "Initial";
   
   // Local variables: camelCase
   var workflowInstance = new WorkflowInstance();
   ```

3. **Method Guidelines**
   - Keep methods small (< 20 lines preferred)
   - Single responsibility
   - Meaningful parameter names
   - Use async/await for I/O operations

4. **Error Handling**
   ```csharp
   // ✅ Good: Specific exceptions
   if (workflow == null)
       throw new WorkflowNotFoundException($"Workflow {id} not found");
   
   // ✅ Good: Proper validation
   if (string.IsNullOrWhiteSpace(request.Id))
       throw new ValidationException("Workflow ID is required");
   ```

### Testing Standards

#### Unit Test Structure
```csharp
[Fact]
public async Task ExecuteAction_WithValidAction_ShouldUpdateState()
{
    // Arrange
    var repository = new InMemoryWorkflowRepository();
    var service = new WorkflowService(repository);
    var workflow = CreateTestWorkflow();
    await repository.SaveDefinitionAsync(workflow);
    
    // Act
    var result = await service.ExecuteActionAsync(instanceId, request);
    
    // Assert
    Assert.Equal("ExpectedState", result.CurrentState);
    Assert.Equal(2, result.History.Count);
}
```

#### Test Naming Convention
```
MethodName_Scenario_ExpectedBehavior

Examples:
- ExecuteAction_WithValidAction_ShouldUpdateState
- CreateWorkflow_WithoutInitialState_ShouldThrowValidationException
- StartInstance_WithValidDefinition_ShouldCreateNewInstance
```

#### Test Categories
```csharp
[Fact] // Simple unit test
[Theory] // Parameterized test
[InlineData("value1", "value2")] // Test data
```

### Documentation Standards

#### XML Documentation
```csharp
/// <summary>
/// Executes a workflow action and transitions the instance to a new state.
/// </summary>
/// <param name="instanceId">The unique identifier of the workflow instance.</param>
/// <param name="request">The action execution request containing the action ID.</param>
/// <returns>The updated workflow instance response.</returns>
/// <exception cref="ValidationException">Thrown when the action cannot be executed.</exception>
/// <exception cref="NotFoundException">Thrown when the instance is not found.</exception>
public async Task<WorkflowInstanceResponse> ExecuteActionAsync(string instanceId, ExecuteActionRequest request)
```

#### README Updates
- Update feature documentation
- Add code examples for new APIs
- Update installation instructions if needed

## 🧪 Testing Guidelines

### Test Coverage Requirements

- **Minimum Coverage**: 80%
- **Critical Paths**: 95%+
- **New Features**: 90%+

### Running Tests

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test category
dotnet test --filter "Category=Unit"

# Run tests with verbose output
dotnet test --verbosity normal
```

### Test Types

1. **Unit Tests**
   - Test individual components in isolation
   - Mock external dependencies
   - Fast execution (< 100ms per test)

2. **Integration Tests**
   - Test component interactions
   - Use real implementations where possible
   - Database/external service integration

3. **API Tests**
   - Test HTTP endpoints
   - Validate request/response formats
   - Error handling scenarios

### Test Data Management

```csharp
public static class TestDataBuilder
{
    public static CreateWorkflowRequest DefaultWorkflowRequest() =>
        new(
            "test-workflow",
            new List<CreateStateDto>
            {
                new("initial", true, false),
                new("final", false, true)
            },
            new List<CreateActionDto>
            {
                new("complete", new List<string> { "initial" }, "final")
            }
        );
}
```

## 📚 Documentation Contributions

### Types of Documentation

1. **API Documentation**: OpenAPI/Swagger specifications
2. **Code Documentation**: XML comments and inline documentation
3. **Architecture Documentation**: Design decisions and patterns
4. **User Documentation**: README, tutorials, examples

### Documentation Standards

- Use clear, concise language
- Include code examples
- Keep documentation up-to-date with code changes
- Use proper markdown formatting

## 🐛 Bug Reports

### Bug Report Template

```markdown
**Describe the bug**
A clear and concise description of what the bug is.

**To Reproduce**
Steps to reproduce the behavior:
1. Go to '...'
2. Click on '....'
3. Scroll down to '....'
4. See error

**Expected behavior**
A clear and concise description of what you expected to happen.

**Screenshots**
If applicable, add screenshots to help explain your problem.

**Environment:**
- OS: [e.g. Windows 10, macOS Big Sur]
- .NET Version: [e.g. 8.0]
- Browser: [e.g. Chrome, Safari] (if applicable)

**Additional context**
Add any other context about the problem here.
```

## ✨ Feature Requests

### Feature Request Template

```markdown
**Is your feature request related to a problem? Please describe.**
A clear and concise description of what the problem is.

**Describe the solution you'd like**
A clear and concise description of what you want to happen.

**Describe alternatives you've considered**
A clear and concise description of any alternative solutions or features you've considered.

**Additional context**
Add any other context or screenshots about the feature request here.
```

## 🔍 Code Review Process

### Review Checklist

**Code Quality:**
- [ ] Code follows style guidelines
- [ ] No code smells or anti-patterns
- [ ] Proper error handling
- [ ] Performance considerations addressed

**Testing:**
- [ ] Adequate test coverage
- [ ] Tests are meaningful and well-named
- [ ] Edge cases covered
- [ ] Integration tests for new features

**Documentation:**
- [ ] Public APIs documented
- [ ] README updated if needed
- [ ] Architecture documents updated
- [ ] Breaking changes documented

**Security:**
- [ ] Input validation implemented
- [ ] No sensitive data exposed
- [ ] SQL injection prevention
- [ ] XSS prevention (if applicable)

### Review Guidelines

1. **Be Constructive**: Provide specific, actionable feedback
2. **Be Respectful**: Focus on code, not the person
3. **Be Thorough**: Check for bugs, performance issues, and maintainability
4. **Be Timely**: Respond to review requests within 2 business days

## 🚀 Release Process

### Version Numbering

We use [Semantic Versioning](https://semver.org/):

- **MAJOR**: Breaking changes
- **MINOR**: New features (backward compatible)
- **PATCH**: Bug fixes (backward compatible)

### Release Checklist

- [ ] All tests pass
- [ ] Documentation updated
- [ ] CHANGELOG updated
- [ ] Version numbers bumped
- [ ] Release notes prepared

## 📞 Getting Help

### Communication Channels

- **GitHub Issues**: Bug reports and feature requests
- **GitHub Discussions**: General questions and community discussions
- **Email**: technical-support@infonetica.com

### Common Questions

**Q: How do I set up the development environment?**
A: Follow the "Development Setup" section above.

**Q: What should I work on?**
A: Check the "good first issue" and "help wanted" labels in GitHub Issues.

**Q: How do I run integration tests?**
A: Run `dotnet test --filter "Category=Integration"`

## 🙏 Recognition

Contributors will be recognized in:
- CONTRIBUTORS.md file
- Release notes
- Project documentation

Thank you for contributing to the Configurable Workflow Engine! 🎉
