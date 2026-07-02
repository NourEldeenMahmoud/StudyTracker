using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudyTracker.Models
{
    /// <summary>
    /// Admin-created badge definitions. Only assignable manually — never awarded automatically.
    /// </summary>
    public class CustomBadgeDefinition
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(255)]
        public string Description { get; set; } = string.Empty;

        /// <summary>Material Symbols name, emoji, or relative URL to an uploaded image.</summary>
        [Required]
        [MaxLength(500)]
        public string Icon { get; set; } = "military_tech";

        /// <summary>Tailwind CSS classes for the icon badge colour.</summary>
        [MaxLength(120)]
        public string Color { get; set; } = "text-yellow-500 bg-yellow-100 dark:bg-yellow-900/30";

        /// <summary>Internal admin note — not shown to users.</summary>
        [MaxLength(500)]
        public string? AdminNote { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public string CreatedByAdminId { get; set; } = string.Empty;

        [ForeignKey(nameof(CreatedByAdminId))]
        public ApplicationUser CreatedByAdmin { get; set; } = null!;
    }
}
