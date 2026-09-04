using System.ComponentModel.DataAnnotations;

namespace LearnSphere.Models.ViewModels
{
    public class CourseFormViewModel
    {
        public int? Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(5000)]
        public string Description { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Thumbnail URL")]
        public string? ThumbnailUrl { get; set; }

        [Required]
        [Display(Name = "Category")]
        public int CategoryId { get; set; }

        public DifficultyLevel Difficulty { get; set; } = DifficultyLevel.Beginner;

        [Range(1, 500)]
        [Display(Name = "Estimated duration (hours)")]
        public int EstimatedDurationHours { get; set; }

        public IEnumerable<Category> Categories { get; set; } = new List<Category>();
    }
}
