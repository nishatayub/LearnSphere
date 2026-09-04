using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace LearnSphere.Models.ViewModels
{
    public class LessonFormViewModel
    {
        public int? Id { get; set; }

        [Required]
        public int CourseId { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [StringLength(2000)]
        public string? Description { get; set; }

        [Display(Name = "Content type")]
        public ContentType ContentType { get; set; } = ContentType.Text;

        [StringLength(10000)]
        [Display(Name = "Text content")]
        public string? Content { get; set; }

        [StringLength(500)]
        [Display(Name = "Content URL (video/PDF)")]
        public string? ContentUrl { get; set; }

        [Display(Name = "Or upload a file (video/PDF)")]
        public IFormFile? UploadedFile { get; set; }

        public string? ExistingContentUrl { get; set; }

        [Range(1, 999)]
        [Display(Name = "Order")]
        public int OrderIndex { get; set; } = 1;

        [Range(0, 600)]
        [Display(Name = "Duration (minutes)")]
        public int DurationMinutes { get; set; }

        [Display(Name = "Free preview")]
        public bool IsFree { get; set; }
    }
}
