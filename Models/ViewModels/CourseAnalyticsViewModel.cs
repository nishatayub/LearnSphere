namespace LearnSphere.Models.ViewModels
{
    public class CourseAnalyticsViewModel
    {
        public Course Course { get; set; } = null!;

        public int TotalEnrollments { get; set; }

        public int ActiveCount { get; set; }

        public int CompletedCount { get; set; }

        public int DroppedCount { get; set; }

        public decimal AverageProgress { get; set; }

        public IEnumerable<LessonStatItem> LessonStats { get; set; } = new List<LessonStatItem>();
    }

    public class LessonStatItem
    {
        public Lesson Lesson { get; set; } = null!;

        public int CompletedCount { get; set; }

        public decimal CompletionRate { get; set; }
    }
}
