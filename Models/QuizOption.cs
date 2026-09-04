using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LearnSphere.Models
{
    public class QuizOption
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int QuizQuestionId { get; set; }

        [Required]
        [StringLength(300)]
        public string Text { get; set; } = string.Empty;

        public bool IsCorrect { get; set; }

        [ForeignKey("QuizQuestionId")]
        public QuizQuestion QuizQuestion { get; set; } = null!;
    }
}
