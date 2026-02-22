using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StudyTracker.Data;
using StudyTracker.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

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
});

builder.Services.AddScoped<StudyTracker.Services.IDashboardService, StudyTracker.Services.DashboardService>();
builder.Services.AddScoped<StudyTracker.Services.IStudySessionService, StudyTracker.Services.StudySessionService>();
builder.Services.AddScoped<StudyTracker.Services.ILeaderboardService, StudyTracker.Services.LeaderboardService>();
builder.Services.AddScoped<StudyTracker.Services.ITargetService, StudyTracker.Services.TargetService>();
builder.Services.AddScoped<StudyTracker.Services.IAdminService, StudyTracker.Services.AdminService>();
builder.Services.AddScoped<StudyTracker.Services.IImageService, StudyTracker.Services.ImageService>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Seed roles and admin user
using (var scope = app.Services.CreateScope())
{
    await DataSeeder.SeedRolesAndAdminAsync(scope.ServiceProvider);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

app.Run();
