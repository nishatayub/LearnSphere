namespace LearnSphere.Models.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }

        public int TotalStudents { get; set; }

        public int TotalInstructors { get; set; }

        public int TotalCourses { get; set; }

        public int PublishedCourses { get; set; }

        public int PendingReviewCourses { get; set; }

        public IEnumerable<Course> CoursesPendingReview { get; set; } = new List<Course>();
    }
}
