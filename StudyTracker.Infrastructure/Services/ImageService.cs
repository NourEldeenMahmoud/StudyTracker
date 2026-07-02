using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;

namespace StudyTracker.Services
{
    public class ImageService : IImageService
    {
        private readonly IWebHostEnvironment _environment;
        private const long MaxFileSize = 5 * 1024 * 1024; // 5MB
        private const int MaxDimension = 2000; // Maximum width or height
        private const int ProfilePictureSize = 200; // Target size for profile pictures
        private const int CompressionQuality = 85; // JPEG quality (0-100)

        // Magic bytes for image validation
        private static readonly byte[] JpegMagicBytes = { 0xFF, 0xD8, 0xFF };
        private static readonly byte[] PngMagicBytes = { 0x89, 0x50, 0x4E, 0x47 };
        private static readonly byte[] GifMagicBytes = { 0x47, 0x49, 0x46, 0x38 };

        public ImageService(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public async Task<string> UploadProfilePictureAsync(IFormFile file, string userId)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is required", nameof(file));

            // Validate file
            if (!await ValidateImageAsync(file))
                throw new InvalidOperationException("Invalid image file");

            // Ensure profiles directory exists
            var profilesFolder = Path.Combine(_environment.WebRootPath, "images", "profiles");
            if (!Directory.Exists(profilesFolder))
            {
                Directory.CreateDirectory(profilesFolder);
            }

            // Generate unique filename
            var fileExtension = ".jpg"; // Always save as JPEG after processing
            var fileName = $"{userId}_{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(profilesFolder, fileName);

            // Copy file to memory stream to avoid stream position issues
            // This allows us to read the stream multiple times after validation
            using (var memoryStream = new MemoryStream())
            {
                await file.CopyToAsync(memoryStream);
                memoryStream.Position = 0; // Reset position to beginning

                // Process and save image
                using (var image = await Image.LoadAsync(memoryStream))
                {
                    // Validate dimensions
                    if (image.Width > MaxDimension || image.Height > MaxDimension)
                    {
                        throw new InvalidOperationException($"Image dimensions must not exceed {MaxDimension}x{MaxDimension} pixels");
                    }

                    // Resize and compress
                    image.Mutate(x => x.Resize(new ResizeOptions
                    {
                        Size = new Size(ProfilePictureSize, ProfilePictureSize),
                        Mode = ResizeMode.Crop // Crop to maintain aspect ratio and fill the size
                    }));

                    // Save as JPEG with compression
                    var encoder = new JpegEncoder
                    {
                        Quality = CompressionQuality
                    };

                    await image.SaveAsync(filePath, encoder);
                }
            }

            // Return relative URL
            return $"/images/profiles/{fileName}";
        }

        public async Task<string> UploadBadgeIconAsync(IFormFile file, string badgeId)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is required", nameof(file));

            if (!await ValidateImageAsync(file))
                throw new InvalidOperationException("Invalid image file");

            var folder = Path.Combine(_environment.WebRootPath, "images", "badges");
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var fileName = $"{badgeId}_{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(folder, fileName);

            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            using var image = await Image.LoadAsync(memoryStream);
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(128, 128),
                Mode = ResizeMode.Pad
            }));

            var encoder = new JpegEncoder { Quality = CompressionQuality };
            await image.SaveAsync(filePath, encoder);

            return $"/images/badges/{Path.GetFileNameWithoutExtension(fileName)}.jpg";
        }

        public async Task DeleteBadgeIconAsync(string? imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl) || !imageUrl.StartsWith("/images/badges/")) return;
            try
            {
                var fileName = Path.GetFileName(imageUrl);
                var filePath = Path.Combine(_environment.WebRootPath, "images", "badges", fileName);
                if (File.Exists(filePath))
                    await Task.Run(() => File.Delete(filePath));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting badge icon: {ex.Message}");
            }
        }

        public async Task DeleteProfilePictureAsync(string? imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl)) return;

            try
            {
                var fileName = Path.GetFileName(imageUrl); // Extract filename from URL
                if (string.IsNullOrEmpty(fileName)) return;

                var profilesFolder = Path.Combine(_environment.WebRootPath, "images", "profiles");
                var filePath = Path.Combine(profilesFolder, fileName);

                // Security check: ensure the file is in the profiles folder
                if (!filePath.StartsWith(profilesFolder, StringComparison.OrdinalIgnoreCase))
                {
                    throw new UnauthorizedAccessException("Invalid file path");
                }

                if (File.Exists(filePath))
                {
                    await Task.Run(() => File.Delete(filePath));
                }
            }
            catch (Exception ex)
            {
                // Log error but don't throw - deletion failure shouldn't break the flow
                System.Diagnostics.Debug.WriteLine($"Error deleting profile picture: {ex.Message}");
            }
        }

        public async Task<bool> ValidateImageAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return false;

            // Check file size
            if (file.Length > MaxFileSize)
                return false;

            // Check file extension
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(fileExtension))
                return false;

            // Validate magic bytes (actual file content)
            try
            {
                using (var stream = file.OpenReadStream())
                {
                    var buffer = new byte[4];
                    var bytesRead = await stream.ReadAsync(buffer, 0, 4);
                    
                    if (bytesRead < 4)
                        return false;

                    // Check for JPEG
                    if (buffer[0] == JpegMagicBytes[0] && 
                        buffer[1] == JpegMagicBytes[1] && 
                        buffer[2] == JpegMagicBytes[2])
                        return true;

                    // Check for PNG
                    if (buffer[0] == PngMagicBytes[0] && 
                        buffer[1] == PngMagicBytes[1] && 
                        buffer[2] == PngMagicBytes[2] && 
                        buffer[3] == PngMagicBytes[3])
                        return true;

                    // Check for GIF
                    if (buffer[0] == GifMagicBytes[0] && 
                        buffer[1] == GifMagicBytes[1] && 
                        buffer[2] == GifMagicBytes[2] && 
                        buffer[3] == GifMagicBytes[3])
                        return true;

                    return false;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
