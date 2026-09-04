using LearnSphere.Controllers;
using LearnSphere.Models;
using LearnSphere.Models.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LearnSphere.Tests
{
    public class InstructorControllerTests : IDisposable
    {
        private readonly TestFixture _fixture;
        private readonly Category _category;
        private readonly User _instructor;
        private readonly User _otherInstructor;

        public InstructorControllerTests()
        {
            _fixture = new TestFixture();
            _instructor = _fixture.CreateUser("owner@test.com", "Instructor");
            _otherInstructor = _fixture.CreateUser("other@test.com", "Instructor");

            _category = new Category { Name = "Programming" };
            _fixture.UnitOfWork.Categories.AddAsync(_category).GetAwaiter().GetResult();
            _fixture.UnitOfWork.SaveChangesAsync().GetAwaiter().GetResult();
        }

        private InstructorController CreateController(User user)
        {
            var controller = new InstructorController(_fixture.UnitOfWork, _fixture.UserManager);
            TestControllerContext.ActAs(controller, user);
            return controller;
        }

        private async Task<Course> CreateDraftCourseAsync(User owner)
        {
            var controller = CreateController(owner);
            await controller.CreateCourse(new CourseFormViewModel
            {
                Title = "New Course",
                Description = "Description",
                CategoryId = _category.Id,
                Difficulty = DifficultyLevel.Beginner,
                EstimatedDurationHours = 5
            });

            return (await _fixture.UnitOfWork.Courses.GetCoursesByInstructorAsync(owner.Id)).Single();
        }

        [Fact]
        public async Task Publish_WithoutAnyLessons_IsRejectedAndCourseStaysDraft()
        {
            var course = await CreateDraftCourseAsync(_instructor);
            var controller = CreateController(_instructor);

            await controller.Publish(course.Id);

            var reloaded = await _fixture.UnitOfWork.Courses.GetByIdAsync(course.Id);
            Assert.Equal(CourseStatus.Draft, reloaded!.Status);
        }

        [Fact]
        public async Task Publish_WithLessons_MovesToUnderReview_NotDirectlyPublished()
        {
            var course = await CreateDraftCourseAsync(_instructor);
            await _fixture.UnitOfWork.Lessons.AddAsync(new Lesson
            {
                CourseVersionId = course.CurrentVersionId!.Value,
                Title = "Lesson 1",
                OrderIndex = 1,
                ContentType = ContentType.Text,
                Content = "Content"
            });
            await _fixture.UnitOfWork.SaveChangesAsync();

            var controller = CreateController(_instructor);
            await controller.Publish(course.Id);

            var reloaded = await _fixture.UnitOfWork.Courses.GetByIdAsync(course.Id);
            Assert.Equal(CourseStatus.UnderReview, reloaded!.Status);
        }

        [Fact]
        public async Task EditCourse_ForCourseOwnedByAnotherInstructor_ReturnsNotFound()
        {
            var course = await CreateDraftCourseAsync(_instructor);
            var controller = CreateController(_otherInstructor);

            var result = await controller.EditCourse(course.Id);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task DeleteLesson_WhenStudentHasProgress_IsBlockedInsteadOfThrowing()
        {
            var course = await CreateDraftCourseAsync(_instructor);
            var lesson = new Lesson
            {
                CourseVersionId = course.CurrentVersionId!.Value,
                Title = "Lesson 1",
                OrderIndex = 1,
                ContentType = ContentType.Text,
                Content = "Content"
            };
            await _fixture.UnitOfWork.Lessons.AddAsync(lesson);
            await _fixture.UnitOfWork.SaveChangesAsync();

            var student = _fixture.CreateUser("student@test.com", "Student");
            var enrollment = new Enrollment
            {
                UserId = student.Id,
                CourseId = course.Id,
                CourseVersionId = course.CurrentVersionId!.Value
            };
            await _fixture.UnitOfWork.Enrollments.AddAsync(enrollment);
            await _fixture.UnitOfWork.SaveChangesAsync();

            await _fixture.UnitOfWork.ProgressRecords.AddAsync(new Progress
            {
                EnrollmentId = enrollment.Id,
                LessonId = lesson.Id,
                IsCompleted = true
            });
            await _fixture.UnitOfWork.SaveChangesAsync();

            var controller = CreateController(_instructor);
            await controller.DeleteLesson(course.Id, lesson.Id);

            var stillExists = await _fixture.UnitOfWork.Lessons.GetByIdAsync(lesson.Id);
            Assert.NotNull(stillExists);
        }

        public void Dispose() => _fixture.Dispose();
    }
}
