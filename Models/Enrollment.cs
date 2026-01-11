using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LearnSphere.Models
{
    public enum EnrollmentStatus
    {
        Active,
        Completed,
        Dropped,
        Suspended
    }

    public class Enrollment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public int CourseId { get; set; }

        [Required]
        public int CourseVersionId { get; set; }

        public DateTime EnrolledDate { get; set; } = DateTime.UtcNow;

        public DateTime? CompletedDate { get; set; }

        public EnrollmentStatus Status { get; set; } = EnrollmentStatus.Active;

        [Column(TypeName = "decimal(5,2)")]
        public decimal ProgressPercentage { get; set; } = 0;

        public DateTime? LastAccessedDate { get; set; }

        // Navigation properties
        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        [ForeignKey("CourseId")]
        public Course Course { get; set; } = null!;

        [ForeignKey("CourseVersionId")]
        public CourseVersion CourseVersion { get; set; } = null!;

        public ICollection<Progress> ProgressRecords { get; set; } = new List<Progress>();
    }
}