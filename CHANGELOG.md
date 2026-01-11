# Changelog

All notable changes to LearnSphere will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Planning
- User authentication system with login/register pages
- Course management UI (create, edit, publish)
- Student enrollment and progress tracking UI
- Certificate generation and verification
- Payment gateway integration
- Real-time notifications

---

## [0.3.0] - 2026-01-11

### Added
- **Repository Pattern Implementation**
  - Generic IRepository<T> interface for CRUD operations
  - Repository<T> base class with EF Core
  - ICourseRepository with course-specific queries
  - CourseRepository with eager loading and filtering
  - IUnitOfWork for transaction management
  - UnitOfWork coordinating multiple repositories
  - Dependency injection registration for all repositories

### Changed
- Architecture now follows clean separation: Controllers → Services → Repositories → Database
- All data access logic abstracted through repository pattern
- Transaction management centralized in Unit of Work

---

## [0.2.0] - 2026-01-11

### Added
- **Database Seeder**
  - DbSeeder class with async methods
  - 3 user roles (Admin, Instructor, Student)
  - 2 test user accounts with secure passwords
  - 4 course categories (Programming, Web Dev, Data Science, Mobile)
  - Sample C# course with 4 lessons
  - Automatic seeding on application startup

### Fixed
- Connection string authentication for Docker SQL Server on macOS
- Circular cascade delete constraint between Course and CourseVersion

---

## [0.1.0] - 2026-01-11

### Added
- **Initial Project Setup**
  - ASP.NET Core MVC project structure
  - Folder organization (Data, Services, Repositories, ViewModels, Utilities)
  - Entity Framework Core and SQL Server packages
  - ASP.NET Core Identity for authentication
  - Docker SQL Server configuration for macOS development

- **Database Foundation**
  - 8 entity models (User, Course, CourseVersion, Lesson, Enrollment, Progress, Certificate, Category)
  - ApplicationDbContext with Fluent API configurations
  - Entity relationships with proper cascade behaviors
  - Database indexes for performance optimization
  - Initial migration and database creation

- **Project Documentation**
  - Comprehensive README with architecture overview
  - CONTRIBUTING guidelines for developers
  - GitHub PR and issue templates
  - MIT License
  - .gitignore for .NET projects
  - CI/CD pipeline with GitHub Actions
  - .env.example for environment variables

### Infrastructure
- Docker SQL Server container setup
- Database connection via Docker on macOS
- Professional Git workflow (main/develop branches)
- Conventional commit message format

---

## Types of Changes

- `Added` - New features
- `Changed` - Changes to existing functionality
- `Deprecated` - Soon-to-be removed features
- `Removed` - Removed features
- `Fixed` - Bug fixes
- `Security` - Security improvements

---

## Version History

**Format:** [MAJOR.MINOR.PATCH]

- **MAJOR** - Incompatible API changes
- **MINOR** - New functionality (backwards compatible)
- **PATCH** - Bug fixes (backwards compatible)

---

[Unreleased]: https://github.com/nishatayub/LearnSphere/compare/v0.1.0...HEAD
[0.1.0]: https://github.com/nishatayub/LearnSphere/releases/tag/v0.1.0
