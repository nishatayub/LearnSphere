using LearnSphere.Controllers;
using LearnSphere.Models;
using Microsoft.AspNetCore.Mvc;

namespace LearnSphere.Tests
{
    public class QuizTests : IDisposable
    {
        private readonly TestFixture _fixture;
        private readonly Category _category;
        private readonly User _instructor;
        private readonly User _student;
        private Course _course = null!;
        private CourseVersion _version = null!;
        private Lesson _quizLesson = null!;
        private Enrollment _enrollment = null!;
        private QuizQuestion _question1 = null!;
        private QuizQuestion _question2 = null!;
        private QuizOption _q1Correct = null!;
        private QuizOption _q1Wrong = null!;
        private QuizOption _q2Correct = null!;
        private QuizOption _q2Wrong = null!;

        public QuizTests()
        {
            _fixture = new TestFixture();
            _instructor = _fixture.CreateUser("instructor@test.com", "Instructor");
            _student = _fixture.CreateUser("student@test.com", "Student");

            _category = new Category { Name = "Programming" };
            _fixture.UnitOfWork.Categories.AddAsync(_category).GetAwaiter().GetResult();
            _fixture.UnitOfWork.SaveChangesAsync().GetAwaiter().GetResult();

            SetupCourseWithQuizAsync().GetAwaiter().GetResult();
        }

        private async Task SetupCourseWithQuizAsync()
        {
            _course = new Course
            {
                Title = "Course",
                Description = "Description",
                InstructorId = _instructor.Id,
                CategoryId = _category.Id,
                Status = CourseStatus.Published
            };
            await _fixture.UnitOfWork.Courses.AddAsync(_course);
            await _fixture.UnitOfWork.SaveChangesAsync();

            _version = new CourseVersion { CourseId = _course.Id, VersionNumber = 1 };
            await _fixture.UnitOfWork.CourseVersions.AddAsync(_version);
            await _fixture.UnitOfWork.SaveChangesAsync();

            _course.CurrentVersionId = _version.Id;
            _fixture.UnitOfWork.Courses.Update(_course);
            await _fixture.UnitOfWork.SaveChangesAsync();

            _quizLesson = new Lesson
            {
                CourseVersionId = _version.Id,
                Title = "Quiz Lesson",
                OrderIndex = 1,
                ContentType = ContentType.Quiz
            };
            await _fixture.UnitOfWork.Lessons.AddAsync(_quizLesson);
            await _fixture.UnitOfWork.SaveChangesAsync();

            _enrollment = new Enrollment { UserId = _student.Id, CourseId = _course.Id, CourseVersionId = _version.Id };
            await _fixture.UnitOfWork.Enrollments.AddAsync(_enrollment);
            await _fixture.UnitOfWork.SaveChangesAsync();

            _question1 = new QuizQuestion { LessonId = _quizLesson.Id, Text = "2 + 2?", OrderIndex = 1 };
            _question2 = new QuizQuestion { LessonId = _quizLesson.Id, Text = "Capital of France?", OrderIndex = 2 };
            await _fixture.UnitOfWork.QuizQuestions.AddAsync(_question1);
            await _fixture.UnitOfWork.QuizQuestions.AddAsync(_question2);
            await _fixture.UnitOfWork.SaveChangesAsync();

            _q1Correct = new QuizOption { QuizQuestionId = _question1.Id, Text = "4", IsCorrect = true };
            _q1Wrong = new QuizOption { QuizQuestionId = _question1.Id, Text = "5", IsCorrect = false };
            _q2Correct = new QuizOption { QuizQuestionId = _question2.Id, Text = "Paris", IsCorrect = true };
            _q2Wrong = new QuizOption { QuizQuestionId = _question2.Id, Text = "London", IsCorrect = false };
            await _fixture.UnitOfWork.QuizOptions.AddAsync(_q1Correct);
            await _fixture.UnitOfWork.QuizOptions.AddAsync(_q1Wrong);
            await _fixture.UnitOfWork.QuizOptions.AddAsync(_q2Correct);
            await _fixture.UnitOfWork.QuizOptions.AddAsync(_q2Wrong);
            await _fixture.UnitOfWork.SaveChangesAsync();
        }

        private CoursesController CreateController()
        {
            var controller = new CoursesController(_fixture.UnitOfWork, _fixture.UserManager);
            TestControllerContext.ActAs(controller, _student);
            return controller;
        }

        [Fact]
        public async Task SubmitQuiz_AllAnswersCorrect_PassesAndCompletesLesson()
        {
            var controller = CreateController();
            var answers = new Dictionary<int, int>
            {
                [_question1.Id] = _q1Correct.Id,
                [_question2.Id] = _q2Correct.Id
            };

            await controller.SubmitQuiz(_course.Id, _quizLesson.Id, answers);

            var attempt = (await _fixture.UnitOfWork.QuizAttempts
                .FindAsync(a => a.EnrollmentId == _enrollment.Id && a.LessonId == _quizLesson.Id))
                .Single();
            var progress = (await _fixture.UnitOfWork.ProgressRecords
                .FindAsync(p => p.EnrollmentId == _enrollment.Id && p.LessonId == _quizLesson.Id))
                .Single();

            Assert.True(attempt.Passed);
            Assert.Equal(100m, attempt.ScorePercentage);
            Assert.True(progress.IsCompleted);
        }

        [Fact]
        public async Task SubmitQuiz_BelowPassingThreshold_DoesNotCompleteLesson()
        {
            var controller = CreateController();
            var answers = new Dictionary<int, int>
            {
                [_question1.Id] = _q1Wrong.Id,
                [_question2.Id] = _q2Correct.Id
            };

            await controller.SubmitQuiz(_course.Id, _quizLesson.Id, answers);

            var attempt = (await _fixture.UnitOfWork.QuizAttempts
                .FindAsync(a => a.EnrollmentId == _enrollment.Id && a.LessonId == _quizLesson.Id))
                .Single();
            var progress = await _fixture.UnitOfWork.ProgressRecords
                .FindAsync(p => p.EnrollmentId == _enrollment.Id && p.LessonId == _quizLesson.Id);

            Assert.False(attempt.Passed);
            Assert.Equal(50m, attempt.ScorePercentage);
            Assert.Empty(progress);
        }

        [Fact]
        public async Task ToggleLesson_OnAQuizLesson_IsRejected()
        {
            var controller = CreateController();

            var result = await controller.ToggleLesson(_course.Id, _quizLesson.Id, null);

            Assert.IsType<BadRequestObjectResult>(result);
            var progress = await _fixture.UnitOfWork.ProgressRecords
                .FindAsync(p => p.EnrollmentId == _enrollment.Id && p.LessonId == _quizLesson.Id);
            Assert.Empty(progress);
        }

        public void Dispose() => _fixture.Dispose();
    }
}
