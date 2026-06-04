using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SmartGear_Online.Models;
using SmartGear_Online.Models.ViewModels;
using System;
using System.Threading.Tasks;

namespace SmartGear_Online.Controllers
{
    /// QUESTION 10: ACCOUNT CONTROLLER
    /// Handles login, registration, logout with Identity
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _logger = logger;
        }

        // ================================================
        // LOGIN GET - Display login form
        // ================================================
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string returnUrl = null)
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // ================================================
        // LOGIN POST - Process login with CSRF protection
        // ================================================
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]  // QUESTION 10: CSRF PROTECTION
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (ModelState.IsValid)
            {
                _logger.LogInformation("Login attempt for user {Email}", model.Email);

                // Lockout on failure - prevents brute force attacks
                var result = await _signInManager.PasswordSignInAsync(
                    model.Email,
                    model.Password,
                    model.RememberMe,
                    lockoutOnFailure: true);

                if (result.Succeeded)
                {
                    // Update last login date
                    var user = await _userManager.FindByEmailAsync(model.Email);
                    if (user != null)
                    {
                        user.LastLoginDate = DateTime.UtcNow;
                        await _userManager.UpdateAsync(user);
                    }

                    _logger.LogInformation("User {Email} logged in successfully", model.Email);

                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }
                    return RedirectToAction("Index", "Home");
                }
                else if (result.IsLockedOut)
                {
                    _logger.LogWarning("User {Email} account locked out due to too many failed attempts", model.Email);
                    ModelState.AddModelError(string.Empty, "Account locked out. Please try again later.");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                }
            }

            return View(model);
        }

        // ================================================
        // REGISTER GET - Display registration form
        // ================================================
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        // ================================================
        // REGISTER POST - Create new user with CSRF protection
        // ================================================
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]  // QUESTION 10: CSRF PROTECTION
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                _logger.LogInformation("Registration attempt for {Email}", model.Email);

                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FullName = model.FullName,
                    PhoneNumber = model.PhoneNumber,
                    DateRegistered = DateTime.UtcNow,
                    IsActive = true
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User {Email} created successfully", model.Email);

                    // Assign "Customer" role by default
                    if (!await _roleManager.RoleExistsAsync("Customer"))
                    {
                        await _roleManager.CreateAsync(new IdentityRole("Customer"));
                    }
                    await _userManager.AddToRoleAsync(user, "Customer");

                    // Auto sign in after registration
                    await _signInManager.SignInAsync(user, isPersistent: false);

                    return RedirectToAction("Index", "Home");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                    _logger.LogWarning("Registration error for {Email}: {Error}", model.Email, error.Description);
                }
            }

            return View(model);
        }

        // ================================================
        // LOGOUT - End user session with CSRF protection
        // ================================================
        [HttpPost]
        [ValidateAntiForgeryToken]  // QUESTION 10: CSRF PROTECTION
        public async Task<IActionResult> Logout()
        {
            var userEmail = User.Identity.Name;
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User {Email} logged out", userEmail);
            return RedirectToAction("Index", "Home");
        }

        // ================================================
        // ACCESS DENIED - Show access denied page
        // ================================================
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        // ================================================
        // PROFILE - View user profile (requires authentication)
        // ================================================
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var model = new ProfileViewModel
            {
                Email = user.Email,
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber,
                DateRegistered = user.DateRegistered,
                LastLoginDate = user.LastLoginDate
            };

            return View(model);
        }
    }
}