using LearnSphere.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LearnSphere.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(ApplicationDbContext context, UserManager<User> userManager, RoleManager<IdentityRole> roleManager)
        {
            // Ensure database is created
            await context.Database.MigrateAsync();

            // Seed Roles
            await SeedRolesAsync(roleManager);

            // Seed Users
            var instructor = await SeedUsersAsync(userManager);

            // Seed Categories
            await SeedCategoriesAsync(context);

            // Seed Courses
            await SeedCoursesAsync(context, instructor);

            await context.SaveChangesAsync();
        }

        private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
        {
            string[] roles = { "Admin", "Instructor", "Student" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }

        private static async Task<User> SeedUsersAsync(UserManager<User> userManager)
        {
            // Create Instructor
            var instructor = await userManager.FindByEmailAsync("instructor@learnsphere.com");
            if (instructor == null)
            {
                instructor = new User
                {
                    UserName = "instructor@learnsphere.com",
                    Email = "instructor@learnsphere.com",
                    FirstName = "John",
                    LastName = "Doe",
                    DateOfBirth = new DateTime(1985, 5, 15),
                    EmailConfirmed = true,
                    Bio = "Experienced software developer and educator with 10+ years in the industry."
                };

                await userManager.CreateAsync(instructor, "Instructor@123");
                await userManager.AddToRoleAsync(instructor, "Instructor");
            }

            // Create Student
            var student = await userManager.FindByEmailAsync("student@learnsphere.com");
            if (student == null)
            {
                student = new User
                {
                    UserName = "student@learnsphere.com",
                    Email = "student@learnsphere.com",
                    FirstName = "Jane",
                    LastName = "Smith",
                    DateOfBirth = new DateTime(2000, 8, 20),
                    EmailConfirmed = true,
                    Bio = "Aspiring software developer eager to learn."
                };

                await userManager.CreateAsync(student, "Student@123");
                await userManager.AddToRoleAsync(student, "Student");
            }

            return instructor;
        }

        private static async Task SeedCategoriesAsync(ApplicationDbContext context)
        {
            if (!await context.Categories.AnyAsync())
            {
                var categories = new List<Category>
                {
                    new Category { Name = "Programming", Description = "Learn various programming languages and frameworks" },
                    new Category { Name = "Web Development", Description = "Master modern web development technologies" },
                    new Category { Name = "Data Science", Description = "Explore data analysis, machine learning, and AI" },
                    new Category { Name = "Mobile Development", Description = "Build mobile applications for iOS and Android" }
                };

                await context.Categories.AddRangeAsync(categories);
                await context.SaveChangesAsync();
            }
        }

        private static async Task SeedCoursesAsync(ApplicationDbContext context, User instructor)
        {
            if (!await context.Courses.AnyAsync())
            {
                var category = await context.Categories.FirstOrDefaultAsync(c => c.Name == "Programming");

                if (category != null)
                {
                    // Create Course
                    var course = new Course
                    {
                        Title = "C# Fundamentals for Beginners",
                        Description = "Learn the basics of C# programming from scratch. Perfect for beginners with no prior programming experience.",
                        InstructorId = instructor.Id,
                        CategoryId = category.Id,
                        Status = CourseStatus.Published,
                        Difficulty = DifficultyLevel.Beginner,
                        EstimatedDurationHours = 10,
                        TotalEnrollments = 0
                    };

                    await context.Courses.AddAsync(course);
                    await context.SaveChangesAsync();

                    // Create Course Version
                    var version = new CourseVersion
                    {
                        CourseId = course.Id,
                        VersionNumber = 1,
                        PublishedDate = DateTime.UtcNow,
                        Changelog = "Initial release",
                        IsActive = true
                    };

                    await context.CourseVersions.AddAsync(version);
                    await context.SaveChangesAsync();

                    // Update Course CurrentVersion
                    course.CurrentVersionId = version.Id;
                    await context.SaveChangesAsync();

                    // Create Lessons
                    var lessons = new List<Lesson>
                    {
                        new Lesson
                        {
                            CourseVersionId = version.Id,
                            Title = "Introduction to C#",
                            Description = "Overview of C# and .NET ecosystem",
                            ContentType = ContentType.Video,
                            OrderIndex = 1,
                            DurationMinutes = 15,
                            IsFree = true
                        },
                        new Lesson
                        {
                            CourseVersionId = version.Id,
                            Title = "Variables and Data Types",
                            Description = "Learn about different data types in C#",
                            ContentType = ContentType.Video,
                            OrderIndex = 2,
                            DurationMinutes = 20,
                            IsFree = false
                        },
                        new Lesson
                        {
                            CourseVersionId = version.Id,
                            Title = "Control Flow Statements",
                            Description = "If statements, loops, and switch cases",
                            ContentType = ContentType.Video,
                            OrderIndex = 3,
                            DurationMinutes = 25,
                            IsFree = false
                        },
                        new Lesson
                        {
                            CourseVersionId = version.Id,
                            Title = "Methods and Functions",
                            Description = "Creating reusable code with methods",
                            ContentType = ContentType.Video,
                            OrderIndex = 4,
                            DurationMinutes = 30,
                            IsFree = false
                        }
                    };

                    await context.Lessons.AddRangeAsync(lessons);
                    await context.SaveChangesAsync();
                }
            }
        }
    }
}