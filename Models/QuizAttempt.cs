using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LearnSphere.Models
{
    public class QuizAttempt
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EnrollmentId { get; set; }

        [Required]
        public int LessonId { get; set; }

        public int TotalQuestions { get; set; }

        public int CorrectAnswers { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal ScorePercentage { get; set; }

        public bool Passed { get; set; }

        public DateTime AttemptedDate { get; set; } = DateTime.UtcNow;

        [ForeignKey("EnrollmentId")]
        public Enrollment Enrollment { get; set; } = null!;

        [ForeignKey("LessonId")]
        public Lesson Lesson { get; set; } = null!;
    }
}
