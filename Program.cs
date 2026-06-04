using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SmartGear_Online.Data;
using SmartGear_Online.Filters;
using SmartGear_Online.Hubs;
using SmartGear_Online.Middleware;
using SmartGear_Online.Models;
using SmartGear_Online.Repositories;
using SmartGear_Online.Services;
using SmartGearOnline.Filters;
using SmartGearOnline.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// ============================================================================
// QUESTION 2: SERVICE CONFIGURATION (Dependency Injection Setup)
// ============================================================================

// Configure Entity Framework Core
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("SmartGearConnection")));

// ============================================================================
// QUESTION 10.4: ASP.NET CORE IDENTITY WITH ENHANCED SECURITY
// ============================================================================

// Configure ASP.NET Core Identity with strict security settings
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password settings - STRONG password requirements
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequiredUniqueChars = 1;

    // Lockout settings - prevents brute force attacks
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.RequireUniqueEmail = true;
    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";

    // Sign-in settings
    options.SignIn.RequireConfirmedEmail = false;
    options.SignIn.RequireConfirmedPhoneNumber = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// ============================================================================
// ORDER SETTINGS CONFIGURATION (NEW - REQUIRED)
// ============================================================================

builder.Services.Configure<OrderSettings>(builder.Configuration.GetSection("OrderSettings"));

// ============================================================================
// QUESTION 10.4: COOKIE AUTHENTICATION CONFIGURATION
// ============================================================================

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.Name = ".SmartGear.Auth";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.Cookie.MaxAge = TimeSpan.FromDays(7);
});

// ============================================================================
// QUESTION 10.4: AUTHORIZATION POLICIES
// ============================================================================

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("CustomerOnly", policy => policy.RequireRole("Customer", "Admin"));
    options.AddPolicy("AuthenticatedUsers", policy => policy.RequireAuthenticatedUser());
    options.AddPolicy("CanManageProducts", policy =>
        policy.RequireAssertion(context =>
            context.User.IsInRole("Admin") ||
            context.User.HasClaim(c => c.Type == "CanManageProducts" && c.Value == "true")));
});

// ============================================================================
// DEPENDENCY INJECTION (UPDATED - Added missing services)
// ============================================================================

builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<ICartRepository, CartRepository>();        
builder.Services.AddScoped<IOrderService, OrderService>();            
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<LoggingActionFilter>();
builder.Services.AddScoped<GlobalExceptionFilter>();

// ============================================================================
// QUESTION 11.1: PERFORMANCE & REAL-TIME FEATURES
// ============================================================================

builder.Services.AddMemoryCache();
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.MaximumReceiveMessageSize = 102400;
    options.StreamBufferCapacity = 10;
});
builder.Services.AddResponseCaching();

// ============================================================================
// MVC & RAZOR PAGES SERVICES
// ============================================================================

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<GlobalExceptionFilter>();
    options.CacheProfiles.Add("Default", new Microsoft.AspNetCore.Mvc.CacheProfile
    {
        Duration = 60,
        Location = Microsoft.AspNetCore.Mvc.ResponseCacheLocation.Any
    });
    options.CacheProfiles.Add("Never", new Microsoft.AspNetCore.Mvc.CacheProfile
    {
        Location = Microsoft.AspNetCore.Mvc.ResponseCacheLocation.None,
        NoStore = true
    });
});

builder.Services.AddRazorPages();

// ============================================================================
// SESSION CONFIGURATION
// ============================================================================

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.Name = ".SmartGear.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

// ============================================================================
// LOGGING CONFIGURATION
// ============================================================================

builder.Services.AddLogging(config =>
{
    config.ClearProviders();
    config.AddConsole();
    config.AddDebug();
    config.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);
});

// Build application
var app = builder.Build();

// ============================================================================
// QUESTION 10.4: SEED ROLES AND ADMIN USER
// ============================================================================

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    await SeedRolesAndAdminAsync(services);
}

// ============================================================================
// MIDDLEWARE PIPELINE
// ============================================================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseResponseCaching();
app.UseAuthentication();
app.UseAuthorization();
app.UseRequestPathLogging();
app.UseMiddleware<SecurityHeadersMiddleware>();

// ============================================================================
// ROUTING
// ============================================================================

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "admin",
    pattern: "admin/{controller=Admin}/{action=Dashboard}/{id?}");

app.MapRazorPages();
app.MapHub<ChatHub>("/chathub");

app.Run();

// ============================================================================
// QUESTION 10.4: SEED ROLES AND ADMIN USER METHOD
// ============================================================================

async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider)
{
    var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    string[] roles = { "Admin", "Customer" };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
            Console.WriteLine($"Role '{role}' created successfully.");
        }
    }

    var adminEmail = "admin@smartgear.com";
    var adminUser = await userManager.FindByEmailAsync(adminEmail);

    if (adminUser == null)
    {
        adminUser = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            FullName = "System Administrator",
            EmailConfirmed = true,
            DateRegistered = DateTime.UtcNow,
            IsActive = true
        };

        var result = await userManager.CreateAsync(adminUser, "Admin@123456");

        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
            Console.WriteLine("Admin user created successfully.");
            Console.WriteLine("Email: admin@smartgear.com");
            Console.WriteLine("Password: Admin@123456");
        }
        else
        {
            foreach (var error in result.Errors)
            {
                Console.WriteLine($"Error creating admin: {error.Description}");
            }
        }
    }
}