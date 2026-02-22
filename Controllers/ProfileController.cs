using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using StudyTracker.Models;
using StudyTracker.Models.ViewModels;
using StudyTracker.Services;

namespace StudyTracker.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IImageService _imageService;

        public ProfileController(
            UserManager<ApplicationUser> userManager, 
            SignInManager<ApplicationUser> signInManager,
            IImageService imageService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _imageService = imageService;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            ViewBag.User = user;
            ViewBag.UserName = user.FullName;
            ViewBag.ProfilePictureUrl = user.ProfilePictureUrl;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            var model = new EditProfileViewModel
            {
                FullName = user.FullName,
                CurrentProfilePictureUrl = user.ProfilePictureUrl
            };

            ViewBag.UserName = user.FullName;
            ViewBag.ProfilePictureUrl = user.ProfilePictureUrl;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditProfileViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                ViewBag.UserName = user.FullName;
                ViewBag.ProfilePictureUrl = user.ProfilePictureUrl;
            }
            
            if (!ModelState.IsValid)
            {
                if (user != null)
                {
                    model.CurrentProfilePictureUrl = user.ProfilePictureUrl;
                }
                return View(model);
            }

            if (user == null)
                return RedirectToAction("Login", "Account");

            // Handle profile picture upload
            if (model.ProfilePicture != null && model.ProfilePicture.Length > 0)
            {
                try
                {
                    // Validate image using ImageService
                    if (!await _imageService.ValidateImageAsync(model.ProfilePicture))
                    {
                        ModelState.AddModelError("ProfilePicture", "Invalid image file. Only JPG, PNG, and GIF files up to 5MB are allowed.");
                        model.CurrentProfilePictureUrl = user.ProfilePictureUrl;
                        return View(model);
                    }

                    // Delete old profile picture if exists
                    if (!string.IsNullOrEmpty(user.ProfilePictureUrl))
                    {
                        await _imageService.DeleteProfilePictureAsync(user.ProfilePictureUrl);
                    }

                    // Upload and process new profile picture
                    user.ProfilePictureUrl = await _imageService.UploadProfilePictureAsync(model.ProfilePicture, user.Id);
                }
                catch (InvalidOperationException ex)
                {
                    ModelState.AddModelError("ProfilePicture", ex.Message);
                    model.CurrentProfilePictureUrl = user.ProfilePictureUrl;
                    return View(model);
                }
                catch (Exception)
                {
                    ModelState.AddModelError("ProfilePicture", "An error occurred while uploading the image. Please try again.");
                    model.CurrentProfilePictureUrl = user.ProfilePictureUrl;
                    return View(model);
                }
            }

            user.FullName = model.FullName;
            await _userManager.UpdateAsync(user);

            TempData["SuccessMessage"] = "Profile updated successfully!";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return NotFound();
                }

                var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);

                if (result.Succeeded)
                {
                    await _signInManager.RefreshSignInAsync(user);
                    TempData["SuccessMessage"] = "Password changed successfully.";
                    return RedirectToAction("Index");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }
    }
}
