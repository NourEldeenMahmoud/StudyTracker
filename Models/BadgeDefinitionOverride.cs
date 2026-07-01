using System.ComponentModel.DataAnnotations;

namespace StudyTracker.Models
{
    /// <summary>
    /// Admin override for a system badge's display (name, description, icon, color).
    /// The badge Key is fixed; only display fields are editable.
    /// </summary>
    public class BadgeDefinitionOverride
    {
        [Key]
        [MaxLength(50)]
        public string BadgeKey { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(255)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string Icon { get; set; } = string.Empty;

        [Required]
        [MaxLength(120)]
        public string Color { get; set; } = string.Empty;

        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
