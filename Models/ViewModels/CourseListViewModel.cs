namespace LearnSphere.Models.ViewModels
{
    public enum CourseSortOrder
    {
        Newest,
        TitleAZ,
        MostEnrolled
    }

    public class CourseListViewModel
    {
        public IEnumerable<Course> Courses { get; set; } = new List<Course>();

        public IEnumerable<Category> Categories { get; set; } = new List<Category>();

        public string? SearchTerm { get; set; }

        public int? CategoryId { get; set; }

        public DifficultyLevel? Difficulty { get; set; }

        public CourseSortOrder Sort { get; set; } = CourseSortOrder.Newest;

        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 6;

        public int TotalCount { get; set; }

        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    }
}
