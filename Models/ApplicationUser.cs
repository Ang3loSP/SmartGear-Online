using Microsoft.AspNetCore.Identity;
using System;
using System.ComponentModel.DataAnnotations;

namespace SmartGear_Online.Models
{
    // Question 10: Application user model
    // Extend the IdentityUser with custom properties
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [StringLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        // ================================================
        // FIXED: Changed to string? to match base IdentityUser (Error 9 fix)
        // ================================================
        [StringLength(20)]
        [Display(Name = "Phone Number")]
        public override string? PhoneNumber { get; set; }  // Changed to nullable string?

        [Display(Name = "Date Registered")]
        public DateTime DateRegistered { get; set; } = DateTime.UtcNow;

        [Display(Name = "Last Login Date")]
        public DateTime? LastLoginDate { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Profile Picture URL")]
        public string ProfilePictureUrl { get; set; } = string.Empty;
    }
}