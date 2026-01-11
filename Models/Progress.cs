using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LearnSphere.Models
{
    public class Progress
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EnrollmentId { get; set; }

        [Required]
        public int LessonId { get; set; }

        public bool IsCompleted { get; set; } = false;

        public DateTime? CompletedDate { get; set; }

        public int TimeSpentMinutes { get; set; } = 0;

        public DateTime LastAccessedDate { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("EnrollmentId")]
        public Enrollment Enrollment { get; set; } = null!;

        [ForeignKey("LessonId")]
        public Lesson Lesson { get; set; } = null!;
    }
}