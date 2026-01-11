using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using LearnSphere.Models;

namespace LearnSphere.Data
{
    public class ApplicationDbContext : IdentityDbContext<User>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DbSets for all entities
        public DbSet<Category> Categories { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<CourseVersion> CourseVersions { get; set; }
        public DbSet<Lesson> Lessons { get; set; }
        public DbSet<Enrollment> Enrollments { get; set; }
        public DbSet<Progress> ProgressRecords { get; set; }
        public DbSet<Certificate> Certificates { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Category Configuration
            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // User Configuration (extends IdentityUser)
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            });

            // Course Configuration
            modelBuilder.Entity<Course>(entity =>
            {
                entity.HasIndex(e => e.Title);
                entity.HasIndex(e => new { e.InstructorId, e.Status });

                // Relationship: Course -> Instructor (User)
                entity.HasOne(c => c.Instructor)
                    .WithMany(u => u.CoursesInstructed)
                    .HasForeignKey(c => c.InstructorId)
                    .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete

                // Relationship: Course -> Category
                entity.HasOne(c => c.Category)
                    .WithMany(cat => cat.Courses)
                    .HasForeignKey(c => c.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Relationship: Course -> CurrentVersion (optional)
                entity.HasOne(c => c.CurrentVersion)
                    .WithMany()
                    .HasForeignKey(c => c.CurrentVersionId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .IsRequired(false);

                // Configure decimal precision
                entity.Property(c => c.AverageRating)
                    .HasColumnType("decimal(3,2)");
            });

            // CourseVersion Configuration
            modelBuilder.Entity<CourseVersion>(entity =>
            {
                entity.HasIndex(e => new { e.CourseId, e.VersionNumber }).IsUnique();

                // Relationship: CourseVersion -> Course
                // Changed to Restrict to avoid circular cascade with Course.CurrentVersion
                entity.HasOne(cv => cv.Course)
                    .WithMany(c => c.Versions)
                    .HasForeignKey(cv => cv.CourseId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Lesson Configuration
            modelBuilder.Entity<Lesson>(entity =>
            {
                entity.HasIndex(e => new { e.CourseVersionId, e.OrderIndex });

                // Relationship: Lesson -> CourseVersion
                entity.HasOne(l => l.CourseVersion)
                    .WithMany(cv => cv.Lessons)
                    .HasForeignKey(l => l.CourseVersionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Enrollment Configuration
            modelBuilder.Entity<Enrollment>(entity =>
            {
                // Composite unique index: One user can only enroll once per course
                entity.HasIndex(e => new { e.UserId, e.CourseId }).IsUnique();
                entity.HasIndex(e => e.EnrolledDate);

                // Relationship: Enrollment -> User
                entity.HasOne(e => e.User)
                    .WithMany(u => u.Enrollments)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Relationship: Enrollment -> Course
                entity.HasOne(e => e.Course)
                    .WithMany(c => c.Enrollments)
                    .HasForeignKey(e => e.CourseId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Relationship: Enrollment -> CourseVersion
                entity.HasOne(e => e.CourseVersion)
                    .WithMany(cv => cv.Enrollments)
                    .HasForeignKey(e => e.CourseVersionId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Configure decimal precision
                entity.Property(e => e.ProgressPercentage)
                    .HasColumnType("decimal(5,2)");
            });

            // Progress Configuration
            modelBuilder.Entity<Progress>(entity =>
            {
                // Composite unique index: One progress record per enrollment per lesson
                entity.HasIndex(e => new { e.EnrollmentId, e.LessonId }).IsUnique();

                // Relationship: Progress -> Enrollment
                entity.HasOne(p => p.Enrollment)
                    .WithMany(e => e.ProgressRecords)
                    .HasForeignKey(p => p.EnrollmentId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Relationship: Progress -> Lesson
                entity.HasOne(p => p.Lesson)
                    .WithMany(l => l.ProgressRecords)
                    .HasForeignKey(p => p.LessonId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Certificate Configuration
            modelBuilder.Entity<Certificate>(entity =>
            {
                // Composite unique index: One certificate per user per course
                entity.HasIndex(e => new { e.UserId, e.CourseId }).IsUnique();
                entity.HasIndex(e => e.VerificationId).IsUnique();

                // Relationship: Certificate -> User
                entity.HasOne(c => c.User)
                    .WithMany(u => u.Certificates)
                    .HasForeignKey(c => c.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Relationship: Certificate -> Course
                entity.HasOne(c => c.Course)
                    .WithMany(course => course.Certificates)
                    .HasForeignKey(c => c.CourseId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}