using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StudyTracker.Data;
using StudyTracker.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password settings
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6;
    
    // User settings
    options.User.RequireUniqueEmail = true;
    
    // Sign in settings
    options.SignIn.RequireConfirmedEmail = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    // Remember Me: persistent cookie lasts 30 days
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
    options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
});

var authSection = builder.Configuration.GetSection("Authentication");
var googleSection = authSection.GetSection("Google");
var discordSection = authSection.GetSection("Discord");

var authBuilder = builder.Services.AddAuthentication();

if (!string.IsNullOrEmpty(googleSection["ClientId"]))
{
    authBuilder.AddGoogle(options =>
    {
        options.ClientId = googleSection["ClientId"]!;
        options.ClientSecret = googleSection["ClientSecret"]!;
    });
}

if (!string.IsNullOrEmpty(discordSection["ClientId"]))
{
    authBuilder.AddDiscord(options =>
    {
        options.ClientId = discordSection["ClientId"]!;
        options.ClientSecret = discordSection["ClientSecret"]!;
        options.Scope.Add("identify");
        options.Scope.Add("email");
    });
}

builder.Services.AddScoped<StudyTracker.Services.IDashboardService, StudyTracker.Services.DashboardService>();
builder.Services.AddScoped<StudyTracker.Services.IStudySessionService, StudyTracker.Services.StudySessionService>();
builder.Services.AddScoped<StudyTracker.Services.ILeaderboardService, StudyTracker.Services.LeaderboardService>();
builder.Services.AddScoped<StudyTracker.Services.ITargetService, StudyTracker.Services.TargetService>();
builder.Services.AddScoped<StudyTracker.Services.IDailyProgressService, StudyTracker.Services.DailyProgressService>();
builder.Services.AddScoped<StudyTracker.Services.IAdminService, StudyTracker.Services.AdminService>();
builder.Services.AddScoped<StudyTracker.Services.IImageService, StudyTracker.Services.ImageService>();
builder.Services.AddScoped<StudyTracker.Services.IBadgeService, StudyTracker.Services.BadgeService>();
builder.Services.AddScoped<StudyTracker.Services.ISecurityLogService, StudyTracker.Services.SecurityLogService>();
builder.Services.AddScoped<StudyTracker.Services.IWeeklyLeaderboardArchiveService, StudyTracker.Services.WeeklyLeaderboardArchiveService>();
builder.Services.AddScoped<StudyTracker.Services.INotificationService, StudyTracker.Services.NotificationService>();

builder.Services.AddMemoryCache();

builder.Services.AddControllersWithViews();

// Remember Me: cookie is already configured with ExpireTimeSpan=30 days and SlidingExpiration.
// On Windows, Data Protection uses a default key storage (e.g. %LOCALAPPDATA%); keys persist across restarts.
// For Linux/Docker, consider configuring key persistence, e.g. AddDataProtection().PersistKeysToFileSystem(...).

var app = builder.Build();

// Automatically apply database migrations and seed data on startup
// Run in background to avoid blocking startup - but use proper scope
try
{
    using (var scope = app.Services.CreateScope())
        {
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            // Step 1: Check database connection (non-blocking)
            logger.LogInformation("Checking database connection...");
            var maxRetries = 3;
            var retryDelay = TimeSpan.FromSeconds(3);
            var connected = false;
            
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    connected = await dbContext.Database.CanConnectAsync();
                    if (connected)
                    {
                        logger.LogInformation("Database connection successful.");
                        break;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning($"Database connection attempt {i + 1}/{maxRetries} failed: {ex.Message}");
                    if (i < maxRetries - 1)
                    {
                        await Task.Delay(retryDelay);
                    }
                }
            }
            
            if (!connected)
            {
                logger.LogError("Cannot connect to database after {MaxRetries} attempts. Application will continue but database features may not work.", maxRetries);
                Console.WriteLine($"WARNING: Database connection failed. Application will continue.");
                // Don't throw - let app continue, but skip migrations and seeding
            }
            else
            {
            
                // Step 2: Get pending migrations (Don't use EnsureCreatedAsync with Migrations)
                try
                {
                    var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
                    var pendingMigrationsList = pendingMigrations.ToList();
                    
                    if (pendingMigrationsList.Any())
                    {
                        logger.LogInformation("Found {Count} pending migration(s): {Migrations}", 
                            pendingMigrationsList.Count, 
                            string.Join(", ", pendingMigrationsList));
                        
                        // Step 3: Apply all pending migrations automatically
                        logger.LogInformation("Applying database migrations...");
                        await dbContext.Database.MigrateAsync();
                        logger.LogInformation("All migrations applied successfully.");
                    }
                    else
                    {
                        logger.LogInformation("Database is up to date. No pending migrations.");
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to apply migrations. Error: {Message}", ex.Message);
                    Console.WriteLine($"ERROR: Migration failed: {ex.Message}");
                    // Don't throw - let app continue
                }
                
                // Step 4: Seed roles and admin user
                try
                {
                    logger.LogInformation("Starting database seeding...");
                    await DataSeeder.SeedRolesAndAdminAsync(scope.ServiceProvider);
                    logger.LogInformation("Database seeding completed successfully.");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to seed database. Error: {Message}", ex.Message);
                    Console.WriteLine($"ERROR: Seeding failed: {ex.Message}");
                    // Don't throw - let app continue
                }

                try
                {
                    var badgeService = scope.ServiceProvider.GetRequiredService<StudyTracker.Services.IBadgeService>();
                    var removed = await badgeService.NormalizeTieredUserBadgesAsync();
                    if (removed > 0)
                        logger.LogInformation("Tiered badges normalized: removed {Count} duplicate lower-tier rows.", removed);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Tiered badge normalization skipped: {Message}", ex.Message);
                }
            }
        }
    }
    catch (Exception ex)
    {
        // Log error but don't crash the app
        try
        {
            var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger<Program>();
            
            logger.LogCritical(ex, 
                "CRITICAL: Database initialization failed!\n" +
                "Error Type: {ErrorType}\n" +
                "Message: {Message}\n" +
                "StackTrace: {StackTrace}\n" +
                "Inner Exception: {InnerException}",
                ex.GetType().Name,
                ex.Message,
                ex.StackTrace,
                ex.InnerException?.Message ?? "None");
        }
        catch
        {
            // If logging fails, at least write to console
            Console.WriteLine($"CRITICAL ERROR: {ex.GetType().Name}");
            Console.WriteLine($"Message: {ex.Message}");
            Console.WriteLine($"StackTrace: {ex.StackTrace}");
        }
        
        // Never throw - let app continue
        Console.WriteLine("Application will continue despite database initialization errors.");
    }

//app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Configure exception handling (must be after UseRouting, UseAuthentication, UseAuthorization)
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

// Add detailed error logging middleware (catches exceptions before they reach exception handler)
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        try
        {
            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, 
                "Unhandled exception in request pipeline. " +
                "Path: {Path}, Method: {Method}, " +
                "Error: {Error}, StackTrace: {StackTrace}",
                context.Request.Path,
                context.Request.Method,
                ex.Message,
                ex.StackTrace);
        }
        catch
        {
            // If logging fails, at least write to console
            Console.WriteLine($"UNHANDLED EXCEPTION: {ex.GetType().Name}");
            Console.WriteLine($"Path: {context.Request.Path}");
            Console.WriteLine($"Method: {context.Request.Method}");
            Console.WriteLine($"Message: {ex.Message}");
            Console.WriteLine($"StackTrace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
            }
        }
        
        // Re-throw to let exception handler handle it
        throw;
    }
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

app.Run();
