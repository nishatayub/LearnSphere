using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LearnSphere.Models
{
    public enum CourseStatus
    {
        Draft,
        UnderReview,
        Published,
        Archived
    }

    public enum DifficultyLevel
    {
        Beginner,
        Intermediate,
        Advanced,
        Expert
    }

    public class Course
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(5000)]
        public string Description { get; set; } = string.Empty;

        [StringLength(500)]
        public string? ThumbnailUrl { get; set; }

        [Required]
        public string InstructorId { get; set; } = string.Empty;

        [Required]
        public int CategoryId { get; set; }

        public CourseStatus Status { get; set; } = CourseStatus.Draft;

        public DifficultyLevel Difficulty { get; set; } = DifficultyLevel.Beginner;

        public int? CurrentVersionId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public int EstimatedDurationHours { get; set; }

        [Column(TypeName = "decimal(3,2)")]
        public decimal? AverageRating { get; set; }

        public int TotalEnrollments { get; set; } = 0;

        // Navigation properties
        [ForeignKey("InstructorId")]
        public User Instructor { get; set; } = null!;

        [ForeignKey("CategoryId")]
        public Category Category { get; set; } = null!;

        [ForeignKey("CurrentVersionId")]
        public CourseVersion? CurrentVersion { get; set; }

        public ICollection<CourseVersion> Versions { get; set; } = new List<CourseVersion>();
        public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
        public ICollection<Certificate> Certificates { get; set; } = new List<Certificate>();
    }
}