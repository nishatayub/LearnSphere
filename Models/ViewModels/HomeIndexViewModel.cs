namespace LearnSphere.Models.ViewModels
{
    public class HomeIndexViewModel
    {
        public IEnumerable<Category> Categories { get; set; } = new List<Category>();

        public IEnumerable<Course> FeaturedCourses { get; set; } = new List<Course>();

        public int TotalPublishedCourses { get; set; }
    }
}
