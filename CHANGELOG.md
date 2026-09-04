# Changelog

All notable changes to LearnSphere will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Planning
- File upload for lesson videos/PDFs
- Email delivery (password reset, course-approval notifications)
- Quiz/assessment content type

---

## [0.7.0] - 2026-09-04 — Docs & Test Coverage

### Added
- xUnit test project (`tests/LearnSphere.Tests`) covering controller-level business
  rules against a real SQLite database with real ASP.NET Core Identity, including
  regression tests for the progress-percentage and version-isolation bugs fixed
  earlier in [0.4.0] and [0.5.0]

### Changed
- Rewrote README/CHANGELOG/CONTRIBUTING to match what's actually implemented -
  the previous README described a service layer, unit/integration tests, and
  several entire fictional subsystems (support tickets, peer reputation,
  spaced-repetition skill retention) that were never built
- Fixed CI (`.github/workflows/dotnet.yml`), which had targeted .NET 8 since before
  the SQLite migration even though the project has been on .NET 9 the whole time

---

## [0.6.0] - 2026-09-04 — Admin Side

### Added
- Admin dashboard with platform-wide stats (users by role, published vs. total courses, pending review count)
- Course approval workflow: instructors submit a Draft for review; admins approve (→ Published) or reject (→ back to Draft)
- Seeded `admin@learnsphere.com` account (the `Admin` role previously existed with nobody assigned to it)
- User management: reassign roles, lock/unlock accounts (both guarded against an admin targeting their own account)
- Category management: create/rename/delete, with deletion blocked while any course still references the category

### Fixed
- `/Account/AccessDenied` (Identity's default `AccessDeniedPath`) had no matching controller action, so a role-restricted route 404'd instead of showing a real page

---

## [0.5.0] - 2026-09-04 — Instructor Side

### Added
- Instructor dashboard for authoring: create/edit courses, add/edit/delete lessons
- Publish workflow with a minimum-one-lesson guard
- Per-course enrollment monitoring (student roster with live progress)
- Per-course analytics: enrollment status breakdown, average progress, per-lesson completion rate
- Course versioning: instructors can publish a new version of a live course without disrupting students already enrolled under the previous version

### Fixed
- `Learn`/`Lesson` pages were reading lessons off `Course.CurrentVersion` instead of `Enrollment.CourseVersionId` — invisible before versioning existed (the two were always equal), but would have silently shifted an enrolled student's content and completion state the moment a course got a second version

---

## [0.4.0] - 2026-09-04 — Student Side Polish

### Added
- Profile editing, change password, forgot/reset password (reset link shown on-screen in lieu of email delivery)
- Real per-lesson content rendering (text, embedded video, PDF link) with Previous/Next navigation — previously the Learn page only listed lesson titles
- Difficulty filter, sort order, and pagination on course browsing

### Fixed
- Course completion percentage wasn't updating after marking a lesson complete — the completed-count query ran against the database before the just-toggled `Progress` row was saved

---

## [0.3.0] - 2026-09-03 — Core Student Loop

### Added
- Registration, login, logout (ASP.NET Core Identity)
- Landing page with live categories and featured courses pulled through the repository layer
- Course browsing, search, and details page
- Enrollment (idempotent — re-enrolling doesn't duplicate or inflate counts)
- Lesson completion tracking and course progress percentage
- Certificates auto-issued on course completion, with public no-login verification by ID

### Fixed
- A real SQL Server password was committed in plaintext in `appsettings.json` from the initial commit; removed and the project switched to SQLite as the default provider so there's no credential to manage locally

---

## [0.2.0] - 2026-01-11

### Added
- Entity models and database schema (Code-First)
- Database seeder with test data
- Repository pattern and Unit of Work

---

## [0.1.0] - 2026-01-11

### Added
- Initial project setup with ASP.NET Core MVC
- Project structure and documentation
- MIT License
- GitHub issue and PR templates
- .gitignore configuration

---

## Types of Changes

- `Added` - New features
- `Changed` - Changes to existing functionality
- `Deprecated` - Soon-to-be removed features
- `Removed` - Removed features
- `Fixed` - Bug fixes
- `Security` - Security improvements

---

[Unreleased]: https://github.com/nishatayub/LearnSphere/compare/main...HEAD
