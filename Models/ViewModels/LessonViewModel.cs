namespace LearnSphere.Models.ViewModels
{
    public class LessonViewModel
    {
        public Course Course { get; set; } = null!;

        public Lesson Lesson { get; set; } = null!;

        public bool IsCompleted { get; set; }

        public int? PreviousLessonId { get; set; }

        public int? NextLessonId { get; set; }
    }
}
