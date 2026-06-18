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


var builder = WebApplication.CreateBuilder(args);

// ============================================================================
// QUESTION 2: SERVICE CONFIGURATION
// ============================================================================

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("SmartGearConnection")));

// ============================================================================
// QUESTION 10.4: IDENTITY
// ============================================================================

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequiredUniqueChars = 1;

    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    options.User.RequireUniqueEmail = true;
    options.User.AllowedUserNameCharacters =
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";

    options.SignIn.RequireConfirmedEmail = false;
    options.SignIn.RequireConfirmedPhoneNumber = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// ============================================================================
// ORDER SETTINGS
// ============================================================================

builder.Services.Configure<OrderSettings>(builder.Configuration.GetSection("OrderSettings"));

// ============================================================================
// QUESTION 10.4: COOKIE AUTHENTICATION
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
// DEPENDENCY INJECTION
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
// PERFORMANCE &amp; REAL-TIME
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
// MVC &amp; RAZOR PAGES
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
// SESSION
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
// LOGGING
// ============================================================================

builder.Services.AddLogging(config =>
{
    config.ClearProviders();
    config.AddConsole();
    config.AddDebug();
    config.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);
});

var app = builder.Build();

// ============================================================================
// QUESTION 10.4: SEED ROLES AND ADMIN USER
// ============================================================================

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    await SeedRolesAndAdminAsync(services, app.Configuration);
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
// FIX: class name corrected after renaming RequestPathLoggingMidddleware to RequestPathLoggingMiddleware
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
// SEED ADMIN — password read from config, never hardcoded in source
// HOW TO SET THE SECRET (run once in the project folder):
//   dotnet user-secrets init
//   dotnet user-secrets set "SeedAdmin:Password" "YourStrongPassword123!"
// ============================================================================

async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider,
                                   IConfiguration configuration)
{
    var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    string[] roles = { "Admin", "Customer" };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
            Console.WriteLine("Role '" + role + "' created successfully.");
        }
    }

    var adminEmail = "admin@smartgear.com";
    var adminUser = await userManager.FindByEmailAsync(adminEmail);

    if (adminUser == null)
    {
        // FIX: password is now read from user-secrets / environment config,
        // not hardcoded in source code.
        var adminPassword = configuration["SeedAdmin:Password"];

        if (string.IsNullOrEmpty(adminPassword))
        {
            Console.WriteLine("WARNING: SeedAdmin:Password is not configured.");
            Console.WriteLine("Run: dotnet user-secrets set \"SeedAdmin:Password\" \"YourPassword!\"");
            return;
        }

        adminUser = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            FullName = "System Administrator",
            EmailConfirmed = true,
            DateRegistered = DateTime.UtcNow,
            IsActive = true
        };

        var result = await userManager.CreateAsync(adminUser, adminPassword);

        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
            Console.WriteLine("Admin user created successfully.");
        }
        else
        {
            foreach (var error in result.Errors)
            {
                Console.WriteLine("Error creating admin: " + error.Description);
            }
        }
    }
}
