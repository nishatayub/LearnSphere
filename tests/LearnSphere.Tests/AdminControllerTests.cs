using LearnSphere.Controllers;
using LearnSphere.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LearnSphere.Tests
{
    public class AdminControllerTests : IDisposable
    {
        private readonly TestFixture _fixture;
        private readonly User _admin;

        public AdminControllerTests()
        {
            _fixture = new TestFixture();
            _admin = _fixture.CreateUser("admin@test.com", "Admin");
        }

        private AdminController CreateController(User actingAs, NullEmailSenderForTests? emailSender = null)
        {
            var controller = new AdminController(_fixture.UnitOfWork, _fixture.UserManager, emailSender ?? new NullEmailSenderForTests());
            TestControllerContext.ActAs(controller, actingAs);
            return controller;
        }

        [Fact]
        public async Task ChangeRole_TargetingYourself_IsRejected()
        {
            var controller = CreateController(_admin);

            await controller.ChangeRole(_admin.Id, "Student");

            var roles = await _fixture.UserManager.GetRolesAsync(_admin);
            Assert.Contains("Admin", roles);
        }

        [Fact]
        public async Task ChangeRole_PromotesStudentToInstructor()
        {
            var student = _fixture.CreateUser("student@test.com", "Student");
            var controller = CreateController(_admin);

            await controller.ChangeRole(student.Id, "Instructor");

            var roles = await _fixture.UserManager.GetRolesAsync(student);
            Assert.Equal(new[] { "Instructor" }, roles);
        }

        [Fact]
        public async Task ToggleLock_TargetingYourself_IsRejected()
        {
            var controller = CreateController(_admin);

            await controller.ToggleLock(_admin.Id);

            var isLockedOut = await _fixture.UserManager.IsLockedOutAsync(_admin);
            Assert.False(isLockedOut);
        }

        [Fact]
        public async Task ToggleLock_LocksAndThenUnlocksAnotherUser()
        {
            var student = _fixture.CreateUser("student@test.com", "Student");
            var controller = CreateController(_admin);

            await controller.ToggleLock(student.Id);
            Assert.True(await _fixture.UserManager.IsLockedOutAsync(student));

            await controller.ToggleLock(student.Id);
            Assert.False(await _fixture.UserManager.IsLockedOutAsync(student));
        }

        [Fact]
        public async Task DeleteCategory_WithCoursesAssigned_IsBlocked()
        {
            var category = new Category { Name = "Programming" };
            await _fixture.UnitOfWork.Categories.AddAsync(category);
            await _fixture.UnitOfWork.SaveChangesAsync();

            var instructor = _fixture.CreateUser("instructor@test.com", "Instructor");
            await _fixture.UnitOfWork.Courses.AddAsync(new Course
            {
                Title = "Course",
                Description = "Description",
                InstructorId = instructor.Id,
                CategoryId = category.Id
            });
            await _fixture.UnitOfWork.SaveChangesAsync();

            var controller = CreateController(_admin);
            await controller.DeleteCategory(category.Id);

            var stillExists = await _fixture.UnitOfWork.Categories.GetByIdAsync(category.Id);
            Assert.NotNull(stillExists);
        }

        [Fact]
        public async Task Approve_PublishesAnUnderReviewCourse()
        {
            var category = new Category { Name = "Programming" };
            await _fixture.UnitOfWork.Categories.AddAsync(category);
            await _fixture.UnitOfWork.SaveChangesAsync();

            var instructor = _fixture.CreateUser("instructor@test.com", "Instructor");
            var course = new Course
            {
                Title = "Course",
                Description = "Description",
                InstructorId = instructor.Id,
                CategoryId = category.Id,
                Status = CourseStatus.UnderReview
            };
            await _fixture.UnitOfWork.Courses.AddAsync(course);
            await _fixture.UnitOfWork.SaveChangesAsync();

            var controller = CreateController(_admin);
            await controller.Approve(course.Id);

            var reloaded = await _fixture.UnitOfWork.Courses.GetByIdAsync(course.Id);
            Assert.Equal(CourseStatus.Published, reloaded!.Status);
        }

        [Fact]
        public async Task Approve_WithEmailConfigured_NotifiesTheInstructor()
        {
            var category = new Category { Name = "Programming" };
            await _fixture.UnitOfWork.Categories.AddAsync(category);
            await _fixture.UnitOfWork.SaveChangesAsync();

            var instructor = _fixture.CreateUser("instructor@test.com", "Instructor");
            var course = new Course
            {
                Title = "Course",
                Description = "Description",
                InstructorId = instructor.Id,
                CategoryId = category.Id,
                Status = CourseStatus.UnderReview
            };
            await _fixture.UnitOfWork.Courses.AddAsync(course);
            await _fixture.UnitOfWork.SaveChangesAsync();

            var emailSender = new NullEmailSenderForTests { IsConfigured = true };
            var controller = CreateController(_admin, emailSender);
            await controller.Approve(course.Id);

            var sent = Assert.Single(emailSender.SentEmails);
            Assert.Equal("instructor@test.com", sent.ToEmail);
            Assert.Contains("approved", sent.Subject);
        }

        [Fact]
        public async Task Approve_WithoutEmailConfigured_DoesNotAttemptToSend()
        {
            var category = new Category { Name = "Programming" };
            await _fixture.UnitOfWork.Categories.AddAsync(category);
            await _fixture.UnitOfWork.SaveChangesAsync();

            var instructor = _fixture.CreateUser("instructor@test.com", "Instructor");
            var course = new Course
            {
                Title = "Course",
                Description = "Description",
                InstructorId = instructor.Id,
                CategoryId = category.Id,
                Status = CourseStatus.UnderReview
            };
            await _fixture.UnitOfWork.Courses.AddAsync(course);
            await _fixture.UnitOfWork.SaveChangesAsync();

            var emailSender = new NullEmailSenderForTests { IsConfigured = false };
            var controller = CreateController(_admin, emailSender);
            await controller.Approve(course.Id);

            Assert.Empty(emailSender.SentEmails);
        }

        public void Dispose() => _fixture.Dispose();
    }
}
