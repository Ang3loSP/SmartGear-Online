using System;
using System.ComponentModel.DataAnnotations;

namespace SmartGear_Online.Models.ViewModels
{
    /// <summary>
    /// QUESTION 10: ViewModel for user profile page
    /// Displays user information and order statistics
    /// </summary>
    public class ProfileViewModel
    {
        [Display(Name = "Email Address")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Display(Name = "Phone Number")]
        [Phone]
        public string PhoneNumber { get; set; } = string.Empty;

        [Display(Name = "Member Since")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:MMMM dd, yyyy}")]
        public DateTime DateRegistered { get; set; }

        [Display(Name = "Last Login")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:MMMM dd, yyyy}")]
        public DateTime? LastLoginDate { get; set; }

        [Display(Name = "Total Orders")]
        public int TotalOrders { get; set; }

        [Display(Name = "Total Spent")]
        [DataType(DataType.Currency)]
        [DisplayFormat(DataFormatString = "{0:C}")]
        public decimal TotalSpent { get; set; }

        [Display(Name = "Average Order Value")]
        [DataType(DataType.Currency)]
        [DisplayFormat(DataFormatString = "{0:C}")]
        public decimal AverageOrderValue { get; set; }

        [Display(Name = "Account Status")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Account Status Text")]
        public string AccountStatus => IsActive ? "Active" : "Inactive";

        [Display(Name = "Profile Completion")]
        public int ProfileCompletionPercentage => CalculateProfileCompletion();

        /// <summary>
        /// Calculates how complete the user profile is (0-100%)
        /// </summary>
        private int CalculateProfileCompletion()
        {
            int completion = 0;

            if (!string.IsNullOrEmpty(FullName)) completion += 25;
            if (!string.IsNullOrEmpty(PhoneNumber)) completion += 25;
            if (!string.IsNullOrEmpty(Email)) completion += 25;
            if (Email?.Contains("@") == true) completion += 25;

            return completion;
        }
    }
}