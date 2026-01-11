using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LearnSphere.Models
{
    public enum ContentType
    {
        Video,
        PDF,
        Text,
        Interactive,
        Quiz
    }

    public class Lesson
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CourseVersionId { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [StringLength(2000)]
        public string? Description { get; set; }

        public ContentType ContentType { get; set; }

        [StringLength(500)]
        public string? ContentUrl { get; set; }

        public int OrderIndex { get; set; }

        public int DurationMinutes { get; set; }

        public bool IsFree { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("CourseVersionId")]
        public CourseVersion CourseVersion { get; set; } = null!;

        public ICollection<Progress> ProgressRecords { get; set; } = new List<Progress>();
    }
}