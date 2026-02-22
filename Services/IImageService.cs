using Microsoft.AspNetCore.Http;

namespace StudyTracker.Services
{
    public interface IImageService
    {
        /// <summary>
        /// Uploads and processes a profile picture
        /// </summary>
        /// <param name="file">The image file to upload</param>
        /// <param name="userId">The user ID for unique filename generation</param>
        /// <returns>The relative URL path to the saved image</returns>
        Task<string> UploadProfilePictureAsync(IFormFile file, string userId);

        /// <summary>
        /// Deletes a profile picture by its URL
        /// </summary>
        /// <param name="imageUrl">The relative URL of the image to delete</param>
        Task DeleteProfilePictureAsync(string? imageUrl);

        /// <summary>
        /// Validates if the file is a valid image
        /// </summary>
        /// <param name="file">The file to validate</param>
        /// <returns>True if valid, false otherwise</returns>
        Task<bool> ValidateImageAsync(IFormFile file);
    }
}
