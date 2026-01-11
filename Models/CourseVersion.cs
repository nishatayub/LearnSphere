using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LearnSphere.Models
{
    public class CourseVersion
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CourseId { get; set; }

        [Required]
        public int VersionNumber { get; set; }

        public DateTime PublishedDate { get; set; } = DateTime.UtcNow;

        [StringLength(2000)]
        public string? Changelog { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation properties
        [ForeignKey("CourseId")]
        public Course Course { get; set; } = null!;

        public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
        public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    }
}