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
                            ContentType = ContentType.Text,
                            Content = "C# is a general-purpose, object-oriented programming language "
                                + "created by Microsoft and built on the .NET platform. It compiles to "
                                + "an intermediate language (IL) that runs on the .NET runtime, which is "
                                + "why the same C# code can run on Windows, macOS, and Linux.\n\n"
                                + "A minimal C# program looks like this:\n\n"
                                + "Console.WriteLine(\"Hello, LearnSphere!\");\n\n"
                                + "That single line is a complete, runnable program - .NET's top-level "
                                + "statements feature removes the need for an explicit Main method and "
                                + "class wrapper for simple programs. Under the hood, the compiler still "
                                + "generates a class with a Main method; it's just hidden from you.\n\n"
                                + "In the next lessons you'll learn how to store data in variables, "
                                + "control the flow of a program, and organize code into reusable methods.",
                            OrderIndex = 1,
                            DurationMinutes = 15,
                            IsFree = true
                        },
                        new Lesson
                        {
                            CourseVersionId = version.Id,
                            Title = "Variables and Data Types",
                            Description = "Learn about different data types in C#",
                            ContentType = ContentType.Text,
                            Content = "C# is statically typed, meaning every variable has a type that's "
                                + "known at compile time. The most common built-in types are:\n\n"
                                + "int age = 25;          // whole numbers\n"
                                + "double price = 19.99;   // decimal numbers\n"
                                + "bool isEnrolled = true; // true or false\n"
                                + "string name = \"Jane\";   // text\n\n"
                                + "You can also let the compiler infer the type with var:\n\n"
                                + "var score = 100; // inferred as int\n\n"
                                + "var doesn't make C# dynamically typed - the variable's type is still "
                                + "fixed at compile time, it's just written for you. Use var when the "
                                + "type is obvious from the right-hand side, and an explicit type when it "
                                + "makes the code clearer to read.",
                            OrderIndex = 2,
                            DurationMinutes = 20,
                            IsFree = false
                        },
                        new Lesson
                        {
                            CourseVersionId = version.Id,
                            Title = "Control Flow Statements",
                            Description = "If statements, loops, and switch cases",
                            ContentType = ContentType.Text,
                            Content = "Control flow statements decide which code runs and how many times.\n\n"
                                + "if / else branches on a condition:\n\n"
                                + "if (score >= 60)\n"
                                + "    Console.WriteLine(\"Pass\");\n"
                                + "else\n"
                                + "    Console.WriteLine(\"Fail\");\n\n"
                                + "for loops repeat a fixed number of times:\n\n"
                                + "for (int i = 0; i < 5; i++)\n"
                                + "    Console.WriteLine(i);\n\n"
                                + "foreach loops iterate over a collection:\n\n"
                                + "foreach (var lesson in lessons)\n"
                                + "    Console.WriteLine(lesson.Title);\n\n"
                                + "switch expressions are a concise way to branch on a single value:\n\n"
                                + "string label = difficulty switch\n"
                                + "{\n"
                                + "    DifficultyLevel.Beginner => \"Easy\",\n"
                                + "    DifficultyLevel.Advanced => \"Hard\",\n"
                                + "    _ => \"Somewhere in between\"\n"
                                + "};",
                            OrderIndex = 3,
                            DurationMinutes = 25,
                            IsFree = false
                        },
                        new Lesson
                        {
                            CourseVersionId = version.Id,
                            Title = "Methods and Functions",
                            Description = "Creating reusable code with methods",
                            ContentType = ContentType.Text,
                            Content = "A method is a named, reusable block of code. It has a return type, "
                                + "a name, and a parameter list:\n\n"
                                + "int Add(int a, int b)\n"
                                + "{\n"
                                + "    return a + b;\n"
                                + "}\n\n"
                                + "Call it like this:\n\n"
                                + "int result = Add(2, 3); // result is 5\n\n"
                                + "If a method doesn't return a value, its return type is void:\n\n"
                                + "void PrintWelcome(string name)\n"
                                + "{\n"
                                + "    Console.WriteLine($\"Welcome, {name}!\");\n"
                                + "}\n\n"
                                + "Breaking logic into small, well-named methods is one of the simplest "
                                + "ways to make a program easier to read, test, and reuse - it's the same "
                                + "idea behind the repository methods you'd find elsewhere in this project, "
                                + "like GetPublishedCoursesAsync() instead of one long block of query code.",
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