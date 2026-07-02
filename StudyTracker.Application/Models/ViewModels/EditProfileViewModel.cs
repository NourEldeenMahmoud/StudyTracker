using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace StudyTracker.Models.ViewModels
{
    public class EditProfileViewModel
    {
        [Required(ErrorMessage = "Full name is required")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;
        
        [Display(Name = "Profile Picture")]
        public IFormFile? ProfilePicture { get; set; }
        
        public string? CurrentProfilePictureUrl { get; set; }
    }
}
