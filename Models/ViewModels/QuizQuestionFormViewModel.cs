using System.ComponentModel.DataAnnotations;

namespace LearnSphere.Models.ViewModels
{
    public class QuizQuestionFormViewModel
    {
        public int? Id { get; set; }

        [Required]
        public int LessonId { get; set; }

        [Required]
        public int CourseId { get; set; }

        [Required]
        [StringLength(500)]
        [Display(Name = "Question")]
        public string Text { get; set; } = string.Empty;

        [Required]
        [StringLength(300)]
        [Display(Name = "Option 1")]
        public string Option1 { get; set; } = string.Empty;

        [Required]
        [StringLength(300)]
        [Display(Name = "Option 2")]
        public string Option2 { get; set; } = string.Empty;

        [StringLength(300)]
        [Display(Name = "Option 3 (optional)")]
        public string? Option3 { get; set; }

        [StringLength(300)]
        [Display(Name = "Option 4 (optional)")]
        public string? Option4 { get; set; }

        [Required]
        [Range(1, 4)]
        [Display(Name = "Correct option")]
        public int CorrectOption { get; set; } = 1;
    }
}
