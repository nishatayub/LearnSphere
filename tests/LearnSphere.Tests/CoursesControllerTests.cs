using LearnSphere.Controllers;
using LearnSphere.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LearnSphere.Tests
{
    public class CoursesControllerTests : IDisposable
    {
        private readonly TestFixture _fixture;
        private readonly Category _category;
        private readonly User _instructor;

        public CoursesControllerTests()
        {
            _fixture = new TestFixture();
            _instructor = _fixture.CreateUser("instructor@test.com", "Instructor");

            _category = new Category { Name = "Programming" };
            _fixture.UnitOfWork.Categories.AddAsync(_category).GetAwaiter().GetResult();
            _fixture.UnitOfWork.SaveChangesAsync().GetAwaiter().GetResult();
        }

        private async Task<(Course course, CourseVersion version, Lesson[] lessons)> CreatePublishedCourseAsync(int lessonCount = 2)
        {
            var course = new Course
            {
                Title = "Course",
                Description = "Description",
                InstructorId = _instructor.Id,
                CategoryId = _category.Id,
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

            var lessons = new Lesson[lessonCount];
            for (var i = 0; i < lessonCount; i++)
            {
                var lesson = new Lesson
                {
                    CourseVersionId = version.Id,
                    Title = $"Lesson {i + 1}",
                    OrderIndex = i + 1,
                    ContentType = ContentType.Text,
                    Content = $"Content {i + 1}"
                };
                await _fixture.UnitOfWork.Lessons.AddAsync(lesson);
                lessons[i] = lesson;
            }
            await _fixture.UnitOfWork.SaveChangesAsync();

            return (course, version, lessons);
        }

        private CoursesController CreateController(User user)
        {
            var controller = new CoursesController(_fixture.UnitOfWork, _fixture.UserManager);
            TestControllerContext.ActAs(controller, user);
            return controller;
        }

        [Fact]
        public async Task ToggleLesson_RecomputesProgressPercentage_InSameRequest()
        {
            var (course, version, lessons) = await CreatePublishedCourseAsync(lessonCount: 4);
            var student = _fixture.CreateUser("student1@test.com", "Student");

            var enrollment = new Enrollment
            {
                UserId = student.Id,
                CourseId = course.Id,
                CourseVersionId = version.Id
            };
            await _fixture.UnitOfWork.Enrollments.AddAsync(enrollment);
            await _fixture.UnitOfWork.SaveChangesAsync();

            var controller = CreateController(student);

            await controller.ToggleLesson(course.Id, lessons[0].Id);

            var updated = await _fixture.UnitOfWork.Enrollments.GetByUserAndCourseAsync(student.Id, course.Id);

            // Regression test: the completed-lesson count used to be queried from the
            // database before the just-toggled Progress row was saved, so this stayed at 0.
            Assert.Equal(25m, updated!.ProgressPercentage);
        }

        [Fact]
        public async Task ToggleLesson_AllLessonsCompleted_MarksEnrollmentCompletedAndIssuesCertificate()
        {
            var (course, version, lessons) = await CreatePublishedCourseAsync(lessonCount: 2);
            var student = _fixture.CreateUser("student2@test.com", "Student");

            var enrollment = new Enrollment { UserId = student.Id, CourseId = course.Id, CourseVersionId = version.Id };
            await _fixture.UnitOfWork.Enrollments.AddAsync(enrollment);
            await _fixture.UnitOfWork.SaveChangesAsync();

            var controller = CreateController(student);
            await controller.ToggleLesson(course.Id, lessons[0].Id);
            await controller.ToggleLesson(course.Id, lessons[1].Id);

            var updated = await _fixture.UnitOfWork.Enrollments.GetByUserAndCourseAsync(student.Id, course.Id);
            var certificate = await _fixture.UnitOfWork.Certificates.GetByUserAndCourseAsync(student.Id, course.Id);

            Assert.Equal(100m, updated!.ProgressPercentage);
            Assert.Equal(EnrollmentStatus.Completed, updated.Status);
            Assert.NotNull(certificate);
        }

        [Fact]
        public async Task Learn_AfterInstructorPublishesNewVersion_StillShowsEnrolledStudentTheOriginalLessons()
        {
            var (course, versionOne, lessons) = await CreatePublishedCourseAsync(lessonCount: 1);
            var student = _fixture.CreateUser("student3@test.com", "Student");

            var enrollment = new Enrollment { UserId = student.Id, CourseId = course.Id, CourseVersionId = versionOne.Id };
            await _fixture.UnitOfWork.Enrollments.AddAsync(enrollment);
            await _fixture.UnitOfWork.SaveChangesAsync();

            // Instructor publishes a new version and renames the lesson the student already has.
            var versionTwo = new CourseVersion { CourseId = course.Id, VersionNumber = 2 };
            await _fixture.UnitOfWork.CourseVersions.AddAsync(versionTwo);
            await _fixture.UnitOfWork.SaveChangesAsync();

            await _fixture.UnitOfWork.Lessons.AddAsync(new Lesson
            {
                CourseVersionId = versionTwo.Id,
                Title = "Renamed in v2",
                OrderIndex = 1,
                ContentType = ContentType.Text,
                Content = "New content"
            });
            course.CurrentVersionId = versionTwo.Id;
            _fixture.UnitOfWork.Courses.Update(course);
            await _fixture.UnitOfWork.SaveChangesAsync();

            var controller = CreateController(student);
            var result = Assert.IsType<ViewResult>(await controller.Learn(course.Id));
            var model = Assert.IsType<LearnSphere.Models.ViewModels.LearnViewModel>(result.Model);

            // Regression test: Learn() used to read Course.CurrentVersion instead of
            // Enrollment.CourseVersionId, so this would have shown "Renamed in v2".
            Assert.Single(model.Lessons);
            Assert.Equal("Lesson 1", model.Lessons.First().Lesson.Title);
        }

        public void Dispose() => _fixture.Dispose();
    }
}
