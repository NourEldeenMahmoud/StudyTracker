using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudyTracker.Models
{
    public class TodoItem
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string Title { get; set; } = string.Empty;

        public bool IsCompleted { get; set; } = false;

        [Required]
        [DataType(DataType.Date)]
        public DateOnly Date { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int Order { get; set; }

        // Navigation property
        [ForeignKey(nameof(UserId))]
        public ApplicationUser User { get; set; } = null!;
    }
}
