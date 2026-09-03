namespace LearnSphere.Models.ViewModels
{
    public class LearnViewModel
    {
        public Course Course { get; set; } = null!;

        public Enrollment Enrollment { get; set; } = null!;

        public IEnumerable<LessonProgressItem> Lessons { get; set; } = new List<LessonProgressItem>();
    }

    public class LessonProgressItem
    {
        public Lesson Lesson { get; set; } = null!;

        public bool IsCompleted { get; set; }
    }
}
