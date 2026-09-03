namespace LearnSphere.Models.ViewModels
{
    public class CourseListViewModel
    {
        public IEnumerable<Course> Courses { get; set; } = new List<Course>();

        public IEnumerable<Category> Categories { get; set; } = new List<Category>();

        public string? SearchTerm { get; set; }

        public int? CategoryId { get; set; }
    }
}
