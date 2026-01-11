# Contributing to LearnSphere 🎓

Thank you for your interest in contributing to LearnSphere! We welcome contributions from the community to help make this LMS even better.

## 📋 Table of Contents

- [Code of Conduct](#code-of-conduct)
- [Getting Started](#getting-started)
- [Development Workflow](#development-workflow)
- [Coding Standards](#coding-standards)
- [Commit Guidelines](#commit-guidelines)
- [Pull Request Process](#pull-request-process)
- [Issue Guidelines](#issue-guidelines)

---

## 🤝 Code of Conduct

By participating in this project, you agree to maintain a respectful and inclusive environment. We expect:

- Respectful communication
- Constructive feedback
- Focus on what's best for the community
- Empathy towards other contributors

---

## 🚀 Getting Started

### Prerequisites

- .NET SDK 6.0 or higher
- SQL Server / PostgreSQL / MySQL
- Visual Studio 2022 or VS Code
- Git

### Setup

1. **Fork the repository**
   ```bash
   # Fork on GitHub, then clone your fork
   git clone https://github.com/YOUR-USERNAME/learnsphere-lms.git
   cd learnsphere-lms
   ```

2. **Add upstream remote**
   ```bash
   git remote add upstream https://github.com/nishatayub/learnsphere-lms.git
   ```

3. **Install dependencies**
   ```bash
   dotnet restore
   ```

4. **Configure database**
   - Copy `appsettings.example.json` to `appsettings.json`
   - Update connection string

5. **Apply migrations**
   ```bash
   dotnet ef database update
   ```

6. **Run the application**
   ```bash
   dotnet run
   ```

---

## 🔄 Development Workflow

### Branch Strategy

We use Git Flow:

```
main          - Production-ready code
  └── develop - Integration branch
        ├── feature/* - New features
        ├── bugfix/*  - Bug fixes
        └── hotfix/*  - Urgent production fixes
```

### Creating a Feature Branch

```bash
# Update your local develop branch
git checkout develop
git pull upstream develop

# Create a new feature branch
git checkout -b feature/your-feature-name

# Work on your feature...

# Push to your fork
git push origin feature/your-feature-name
```

### Branch Naming Convention

- `feature/user-authentication` - New features
- `bugfix/enrollment-validation` - Bug fixes
- `hotfix/security-patch` - Urgent fixes
- `refactor/service-layer` - Code refactoring
- `docs/api-documentation` - Documentation

---

## 💻 Coding Standards

### C# Conventions

Follow Microsoft's C# coding conventions:

```csharp
// ✅ Good - PascalCase for classes, methods, properties
public class CourseService
{
    public async Task<Course> GetCourseByIdAsync(int id)
    {
        // camelCase for local variables
        var course = await _repository.GetByIdAsync(id);
        return course;
    }
}

// ✅ Use meaningful names
public class EnrollmentValidator
{
    private readonly IEnrollmentRepository _enrollmentRepository;
    
    // Constructor injection
    public EnrollmentValidator(IEnrollmentRepository enrollmentRepository)
    {
        _enrollmentRepository = enrollmentRepository;
    }
}
```

### Architecture Guidelines

- **Controllers** - Handle HTTP requests only, delegate to services
- **Services** - Contain business logic
- **Repositories** - Data access only
- **ViewModels** - For passing data to views
- **Models** - Entity classes

### Best Practices

✅ **DO:**
- Use async/await for I/O operations
- Implement proper error handling
- Add XML documentation comments
- Write unit tests for services
- Use dependency injection
- Follow SOLID principles

❌ **DON'T:**
- Put business logic in controllers
- Use magic numbers or strings
- Ignore compiler warnings
- Commit sensitive data
- Write overly complex methods

---

## 📝 Commit Guidelines

We follow [Conventional Commits](https://www.conventionalcommits.org/):

### Format

```
<type>(<scope>): <subject>

<body>

<footer>
```

### Types

- `feat` - New feature
- `fix` - Bug fix
- `docs` - Documentation changes
- `style` - Code formatting (no logic change)
- `refactor` - Code restructuring
- `test` - Adding/updating tests
- `chore` - Build process, dependencies
- `perf` - Performance improvements

### Examples

```bash
# Feature
git commit -m "feat(auth): implement JWT token refresh mechanism"

# Bug fix
git commit -m "fix(enrollment): prevent duplicate course enrollments"

# With body
git commit -m "refactor(services): implement repository pattern

- Create generic repository interface
- Implement unit of work pattern
- Update service layer to use repositories

Closes #45"
```

### Commit Message Rules

- Use present tense ("add feature" not "added feature")
- Use imperative mood ("move cursor to..." not "moves cursor to...")
- First line should be 50 characters or less
- Reference issues and PRs in the footer

---

## 🔍 Pull Request Process

### Before Submitting

1. **Update your branch**
   ```bash
   git checkout develop
   git pull upstream develop
   git checkout feature/your-feature
   git rebase develop
   ```

2. **Run tests**
   ```bash
   dotnet test
   ```

3. **Build successfully**
   ```bash
   dotnet build --configuration Release
   ```

4. **Self-review your code**

### Submitting a PR

1. **Push to your fork**
   ```bash
   git push origin feature/your-feature-name
   ```

2. **Create Pull Request on GitHub**
   - Use the PR template
   - Provide clear description
   - Link related issues
   - Add screenshots if UI changes

3. **Address review comments**
   - Make requested changes
   - Push updates to the same branch
   - Respond to reviewer feedback

### PR Requirements

- [ ] All tests pass
- [ ] No merge conflicts
- [ ] Code follows style guidelines
- [ ] Documentation updated
- [ ] Commit messages follow convention
- [ ] PR description is complete

### Review Process

1. Automated checks (CI/CD) must pass
2. At least one maintainer approval required
3. All review comments addressed
4. Branch is up to date with develop

---

## 🐛 Issue Guidelines

### Before Creating an Issue

1. **Search existing issues** - Avoid duplicates
2. **Check documentation** - Your question might be answered
3. **Try latest version** - Issue might be fixed

### Creating an Issue

Use the appropriate template:

- **🐛 Bug Report** - For bugs and errors
- **✨ Feature Request** - For new features
- **📚 Documentation** - For documentation improvements
- **❓ Question** - For general questions

### Writing Good Issues

**Bug Reports should include:**
- Clear title
- Steps to reproduce
- Expected vs actual behavior
- Environment details
- Screenshots if applicable

**Feature Requests should include:**
- Problem statement
- Proposed solution
- User impact
- Acceptance criteria

---

## 🧪 Testing

### Running Tests

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test tests/LearnSphere.Tests

# Run with coverage
dotnet test /p:CollectCoverage=true
```

### Writing Tests

```csharp
[Fact]
public async Task EnrollStudent_ValidCourse_ReturnsEnrollment()
{
    // Arrange
    var service = new EnrollmentService(_mockRepository.Object);
    var studentId = 1;
    var courseId = 100;
    
    // Act
    var result = await service.EnrollStudentAsync(studentId, courseId);
    
    // Assert
    Assert.NotNull(result);
    Assert.Equal(studentId, result.StudentId);
}
```

---

## 🎨 Code Style

### EditorConfig

We use `.editorconfig` for consistent formatting:

```ini
# .editorconfig
root = true

[*]
indent_style = space
indent_size = 4
end_of_line = lf
charset = utf-8
trim_trailing_whitespace = true
insert_final_newline = true

[*.cs]
csharp_new_line_before_open_brace = all
csharp_indent_case_contents = true
```

### Formatting

Use Visual Studio's auto-formatting:
- Windows: `Ctrl + K, Ctrl + D`
- macOS: `Cmd + K, Cmd + D`

---

## 📚 Additional Resources

- [Project README](README.md)
- [Architecture Documentation](docs/ARCHITECTURE.md)
- [API Documentation](docs/API.md)
- [Deployment Guide](docs/DEPLOYMENT.md)

---

## 🙏 Recognition

Contributors will be recognized in:
- README.md contributors section
- Release notes
- Project documentation

Thank you for contributing to LearnSphere! 🎓✨

---

## 📞 Questions?

- Open a [Discussion](../../discussions)
- Create an [Issue](../../issues/new)
- Contact maintainers

**Happy Coding! 🚀**
