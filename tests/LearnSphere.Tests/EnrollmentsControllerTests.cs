using LearnSphere.Controllers;
using LearnSphere.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LearnSphere.Tests
{
    public class EnrollmentsControllerTests : IDisposable
    {
        private readonly TestFixture _fixture;

        public EnrollmentsControllerTests()
        {
            _fixture = new TestFixture();
        }

        private EnrollmentsController CreateController(User user)
        {
            var controller = new EnrollmentsController(_fixture.UnitOfWork, _fixture.UserManager);
            TestControllerContext.ActAs(controller, user);
            return controller;
        }

        private async Task<Course> CreatePublishedCourseAsync()
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
                Status = CourseStatus.Published
            };
            await _fixture.UnitOfWork.Courses.AddAsync(course);
            await _fixture.UnitOfWork.SaveChangesAsync();

            var version = new CourseVersion { CourseId = course.Id, VersionNumber = 1 };
            await _fixture.UnitOfWork.CourseVersions.AddAsync(version);
            await _fixture.UnitOfWork.SaveChangesAsync();

            course.CurrentVersionId = version.Id;
            _fixture.UnitOfWork.Courses.Update(course);
            await _fixture.UnitOfWork.SaveChangesAsync();

            return course;
        }

        [Fact]
        public async Task Enroll_CalledTwice_DoesNotDuplicateEnrollmentOrInflateCount()
        {
            var course = await CreatePublishedCourseAsync();
            var student = _fixture.CreateUser("student@test.com", "Student");
            var controller = CreateController(student);

            await controller.Enroll(course.Id);
            await controller.Enroll(course.Id);

            var enrollments = await _fixture.UnitOfWork.Enrollments.GetByUserIdAsync(student.Id);
            var reloadedCourse = await _fixture.UnitOfWork.Courses.GetByIdAsync(course.Id);

            Assert.Single(enrollments);
            Assert.Equal(1, reloadedCourse!.TotalEnrollments);
        }

        public void Dispose() => _fixture.Dispose();
    }
}
