<div align="center">

# 🎓 LearnSphere

### *A Role-Based Learning Management System*

Built with ASP.NET Core MVC

---

[![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-512BD4?style=for-the-badge&logo=.net&logoColor=white)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![Entity Framework](https://img.shields.io/badge/Entity%20Framework-512BD4?style=for-the-badge&logo=.net&logoColor=white)](https://docs.microsoft.com/en-us/ef/)

---

</div>

## 📖 Table of Contents

- [Overview](#-overview)
- [User Roles & Capabilities](#-user-roles--capabilities)
- [Architecture](#-architecture)
- [Technology Stack](#-technology-stack)
- [Database Schema](#-database-schema)
- [Getting Started](#-getting-started)
- [Seeded Demo Accounts](#-seeded-demo-accounts)
- [Testing](#-testing)
- [What's Not Built Yet](#-whats-not-built-yet)
- [Roadmap](#-roadmap)
- [License](#-license)
- [About the Developer](#-about-the-developer)

---

## 📚 Overview

**LearnSphere** is a full-stack Learning Management System built with **ASP.NET Core MVC**, covering the complete loop for three roles: **students** browse and complete courses, **instructors** author and publish content, and **admins** review and moderate the platform. Every feature described below is implemented and has been manually verified end-to-end in a running instance — nothing here is aspirational.

The project uses **ASP.NET Core Identity** for auth, **Entity Framework Core** (Code-First migrations) with **SQLite** by default so it runs with zero setup, and a **Repository + Unit of Work** pattern for data access.

---

## 👥 User Roles & Capabilities

### 👤 Student

| Feature | Details |
|---------|---------|
| **Account** | Register, log in, edit profile, change password, forgot/reset password |
| **Course Discovery** | Search by keyword, filter by category and difficulty, sort (newest/title/most enrolled), paginated results |
| **Enrollment** | Enroll in a published course (duplicate-enrollment guarded) |
| **Learning** | Per-lesson content view (text, embedded video, or PDF link), Previous/Next navigation, mark-complete toggle |
| **Progress** | Live completion percentage recalculated from actual lesson completions, "My Courses" dashboard |
| **Certificates** | Auto-issued on 100% completion, listed under "My Certificates", publicly verifiable by ID with no login required |

### 👨‍🏫 Instructor

| Feature | Details |
|---------|---------|
| **Course Authoring** | Create/edit courses (title, description, category, difficulty, duration); starts as Draft |
| **Lesson Authoring** | Add/edit/delete lessons (text content, video/PDF URL, order, duration, free-preview flag) |
| **Publish Workflow** | Submit a Draft for admin review; can't submit without at least one lesson |
| **Enrollment Monitoring** | Per-course list of enrolled students with status and live progress |
| **Analytics** | Enrollment counts by status, average progress, per-lesson completion-rate breakdown |
| **Versioning** | Publish a new version of a live course — existing enrollments keep the version they started on, new enrollments and the public listing get the update |

### 🛡️ Admin

| Feature | Details |
|---------|---------|
| **Dashboard** | Platform totals: users by role, published vs. total courses, courses pending review |
| **Course Approval** | Review a submitted course (full content preview) and approve (→ Published) or reject (→ back to Draft) |
| **User Management** | Reassign roles (Student/Instructor/Admin), lock/unlock accounts — both guarded against targeting your own account |
| **Category Management** | Create, rename, delete categories; deletion is blocked while any course still references the category |

---

## 🏛️ Architecture

- **MVC controllers** call directly into a **Repository + Unit of Work** layer — there is no separate service layer; business rules (ownership checks, publish guards, version isolation) live in the controllers.
- Each aggregate with custom queries (`Course`, `Enrollment`, `Certificate`) has its own repository interface extending a generic `IRepository<T>`; everything else uses the generic repository directly.
- `IUnitOfWork` composes the repositories and exposes a single `SaveChangesAsync()`, so a request that touches multiple tables (e.g. enrolling + bumping a course's enrollment count) commits together.
- Authorization is role-based (`[Authorize(Roles = "...")]`) via ASP.NET Core Identity, not claims-based.

```
Controllers (MVC)
    ↓ calls
IUnitOfWork → ICourseRepository, IEnrollmentRepository, ICertificateRepository, IRepository<T>
    ↓ backed by
ApplicationDbContext (EF Core, Code-First migrations)
```

---

## 🛠️ Technology Stack

| Layer | Technology |
|-------|------------|
| Framework | ASP.NET Core MVC (.NET 9) |
| Language | C# |
| ORM | Entity Framework Core, Code-First migrations |
| Auth | ASP.NET Core Identity (cookie-based, role-based authorization) |
| Database | SQLite by default (zero setup); swappable for SQL Server |
| Frontend | Razor views, Bootstrap 5 |

---

## 🗄️ Database Schema

```
User (extends IdentityUser)
├── FirstName, LastName, DateOfBirth, Bio, ProfilePictureUrl
├── CreatedAt, LastLoginAt
└── Roles: Student / Instructor / Admin

Category
├── Name (unique), Description

Course
├── Title, Description, ThumbnailUrl
├── InstructorId (FK → User), CategoryId (FK → Category)
├── Status: Draft / UnderReview / Published / Archived
├── Difficulty, EstimatedDurationHours
├── CurrentVersionId (FK → CourseVersion)
└── TotalEnrollments, AverageRating

CourseVersion
├── CourseId (FK → Course), VersionNumber, Changelog, IsActive

Lesson
├── CourseVersionId (FK → CourseVersion)
├── Title, Description, OrderIndex, DurationMinutes, IsFree
├── ContentType: Video / PDF / Text / Interactive / Quiz
└── ContentUrl, Content (inline text body)

Enrollment
├── UserId (FK → User), CourseId (FK → Course)
├── CourseVersionId (FK → CourseVersion) — locked at enroll time
├── Status: Active / Completed / Dropped / Suspended
└── ProgressPercentage, EnrolledDate, CompletedDate

Progress
├── EnrollmentId (FK → Enrollment), LessonId (FK → Lesson)
└── IsCompleted, CompletedDate, TimeSpentMinutes

Certificate
├── UserId (FK → User), CourseId (FK → Course)
├── VerificationId (unique)
└── IssuedDate
```

**Why `Enrollment.CourseVersionId` is locked at enroll time:** when an instructor publishes a new version of a course, `Course.CurrentVersionId` moves forward, but each `Enrollment` keeps pointing at the version it was created against. This is what lets an instructor update course content without silently changing what an in-progress student sees.

---

## 🚀 Getting Started

### Prerequisites

- **.NET SDK 9.0+**
- **Git**

No database server is required — SQLite runs as a local file.

### Installation

```bash
git clone https://github.com/nishatayub/LearnSphere.git
cd LearnSphere
dotnet restore
dotnet ef database update
dotnet run
```

The app seeds itself with demo data (categories, one course, and one account per role) on first run — see below.

---

## 🔑 Seeded Demo Accounts

| Role | Email | Password |
|------|-------|----------|
| Admin | `admin@learnsphere.com` | `Admin@123` |
| Instructor | `instructor@learnsphere.com` | `Instructor@123` |
| Student | `student@learnsphere.com` | `Student@123` |

Use these to explore all three roles without registering new accounts. New self-registration always creates a **Student** — an admin has to promote an account to Instructor via Manage Users.

---

## 🧪 Testing

`tests/LearnSphere.Tests` is an xUnit project that runs controller-level tests against a real SQLite database (in-memory, one connection per test) with a real ASP.NET Core Identity stack behind it — not a mock. Coverage focuses on the business rules controllers enforce and, specifically, on two real bugs that were caught and fixed during manual verification: progress percentage not recomputing correctly in a single request, and the Learn page reading lessons from the course's current version instead of the version a student actually enrolled under.

```bash
dotnet test tests/LearnSphere.Tests/LearnSphere.Tests.csproj
```

This isn't full coverage — it doesn't touch Razor views, Identity's built-in flows (login/register/password reset), or file-level repository behavior beyond what the controller tests exercise incidentally. Everything else in this README was verified manually against a running instance.

---

## ⚠️ What's Not Built Yet

Being upfront about scope, since a fair number of student LMS projects overclaim here:

- **No file uploads.** Lessons hold text content or an external URL (e.g. a hosted video link) — there's no upload/storage pipeline for video or PDF files.
- **No prerequisite system.** Courses don't declare dependencies on other courses.
- **No real email delivery.** "Forgot password" generates a real Identity reset token but displays the link on-screen instead of emailing it (no SMTP configured).
- **No quizzes/assignments.** `ContentType.Quiz` exists on the `Lesson` enum but there's no quiz-taking or grading feature behind it.
- **No discussion/forum feature.**

---

## 🗺️ Roadmap

Realistic next steps, roughly in priority order:

- Broaden test coverage to Identity flows and repositories directly
- Real file upload for lesson videos/PDFs (local disk or blob storage)
- Email delivery for password reset and course-approval notifications
- Quiz/assessment content type
- API layer for a future mobile client

---

## 📜 License

This project is licensed for **educational and demonstration purposes**.

```
Copyright © 2026 Nishat Ayub
All rights reserved.

This software is provided for educational purposes only.
Commercial use requires explicit permission.
```

---

## 👨‍💻 About the Developer

<div align="center">

### Nishat Ayub

**Aspiring Software Engineer | Backend & Full-Stack Developer**

[![LinkedIn](https://img.shields.io/badge/LinkedIn-0077B5?style=for-the-badge&logo=linkedin&logoColor=white)](https://linkedin.com/in/nishatayub)
[![GitHub](https://img.shields.io/badge/GitHub-100000?style=for-the-badge&logo=github&logoColor=white)](https://github.com/nishatayub)
[![Portfolio](https://img.shields.io/badge/Portfolio-FF5722?style=for-the-badge&logo=google-chrome&logoColor=white)](https://nishatayub.vercel.app)

</div>
