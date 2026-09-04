using System.ComponentModel.DataAnnotations;

namespace LearnSphere.Models.ViewModels
{
    public class NewVersionViewModel
    {
        [Required]
        public int CourseId { get; set; }

        [Required]
        [StringLength(2000)]
        [Display(Name = "What changed in this version?")]
        public string Changelog { get; set; } = string.Empty;
    }
}
