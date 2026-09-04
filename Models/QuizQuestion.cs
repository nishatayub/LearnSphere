using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LearnSphere.Models
{
    public class QuizQuestion
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int LessonId { get; set; }

        [Required]
        [StringLength(500)]
        public string Text { get; set; } = string.Empty;

        public int OrderIndex { get; set; }

        [ForeignKey("LessonId")]
        public Lesson Lesson { get; set; } = null!;

        public ICollection<QuizOption> Options { get; set; } = new List<QuizOption>();
    }
}
