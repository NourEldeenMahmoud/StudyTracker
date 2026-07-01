using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using StudyTracker.Models;

namespace StudyTracker.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Dashboard");
        }
        return RedirectToAction("Login", "Account");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        var requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        
        // Log the error details
        var exceptionHandlerPathFeature = HttpContext.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();
        if (exceptionHandlerPathFeature?.Error != null)
        {
            var exception = exceptionHandlerPathFeature.Error;
            _logger.LogError(exception, 
                "Error occurred. RequestId: {RequestId}, Path: {Path}",
                requestId,
                exceptionHandlerPathFeature.Path);
            
            // Write to console for stdout logs
            Console.WriteLine($"ERROR PAGE - RequestId: {requestId}");
            Console.WriteLine($"Path: {exceptionHandlerPathFeature.Path}");
            Console.WriteLine($"Error Type: {exception.GetType().Name}");
            Console.WriteLine($"Error Message: {exception.Message}");
            Console.WriteLine($"StackTrace: {exception.StackTrace}");
            if (exception.InnerException != null)
            {
                Console.WriteLine($"Inner Exception: {exception.InnerException.Message}");
            }
        }
        
        return View(new ErrorViewModel { RequestId = requestId });
    }
}
