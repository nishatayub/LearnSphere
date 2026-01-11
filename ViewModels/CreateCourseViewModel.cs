using System.ComponentModel.DataAnnotations;
using LearnSphere.Models;

namespace LearnSphere.ViewModels
{
    /// <summary>
    /// ViewModel for creating a new course.
    /// Separates view input from domain model.
    /// </summary>
    public class CreateCourseViewModel
    {
        [Required(ErrorMessage = "Course title is required")]
        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
        [Display(Name = "Course Title")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
        [Display(Name = "Description")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Category is required")]
        [Display(Name = "Category")]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Difficulty level is required")]
        [Display(Name = "Difficulty Level")]
        public DifficultyLevel DifficultyLevel { get; set; }

        [StringLength(500)]
        [Display(Name = "Thumbnail URL")]
        public string? ThumbnailUrl { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Price must be positive")]
        [Display(Name = "Price (leave 0 for free)")]
        public decimal Price { get; set; } = 0;
    }
}
