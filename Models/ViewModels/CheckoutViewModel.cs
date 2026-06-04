using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SmartGear_Online.Models.ViewModels
{
    /// <summary>
    /// ViewModel for checkout page
    /// Contains cart items and shipping/billing information
    /// </summary>
    public class CheckoutViewModel
    {
        // Cart information
        public List<CartItem> CartItems { get; set; } = new List<CartItem>();
        public decimal Subtotal { get; set; }
        public decimal Tax { get; set; }
        public decimal ShippingCost { get; set; }
        public decimal GrandTotal { get; set; }
        public string DiscountCode { get; set; } = string.Empty;
        public decimal DiscountAmount { get; set; }

        // Shipping information
        [Required(ErrorMessage = "Full name is required")]
        [StringLength(100, MinimumLength = 2)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required")]
        [Phone(ErrorMessage = "Invalid phone number")]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Street address is required")]
        [StringLength(200)]
        [Display(Name = "Street Address")]
        public string StreetAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "City is required")]
        [StringLength(50)]
        public string City { get; set; } = string.Empty;

        [Required(ErrorMessage = "State/Province is required")]
        [StringLength(50)]
        [Display(Name = "State/Province")]
        public string State { get; set; } = string.Empty;

        [Required(ErrorMessage = "Postal/ZIP code is required")]
        [StringLength(20)]
        [Display(Name = "Postal/ZIP Code")]
        public string PostalCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Country is required")]
        [StringLength(50)]
        public string Country { get; set; } = "United States";

        // Shipping method
        [Required]
        public string ShippingMethod { get; set; } = "Standard";

        // FIXED: Added ShippingAddress property
        public string ShippingAddress { get; set; } = string.Empty;

        // FIXED: Added BillingAddress property
        public string BillingAddress { get; set; } = string.Empty;

        // Billing information (same as shipping by default)
        public bool SameAsShipping { get; set; } = true;

        [StringLength(200)]
        [Display(Name = "Billing Street Address")]
        public string BillingStreetAddress { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "Billing City")]
        public string BillingCity { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "Billing State/Province")]
        public string BillingState { get; set; } = string.Empty;

        [StringLength(20)]
        [Display(Name = "Billing Postal/ZIP Code")]
        public string BillingPostalCode { get; set; } = string.Empty;

        // Payment information
        [Required(ErrorMessage = "Card number is required")]
        [CreditCard(ErrorMessage = "Invalid card number")]
        [Display(Name = "Card Number")]
        public string CardNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Expiry date is required")]
        [RegularExpression(@"^(0[1-9]|1[0-2])\/([0-9]{2})$",
            ErrorMessage = "Expiry date must be in MM/YY format")]
        [Display(Name = "Expiry Date (MM/YY)")]
        public string ExpiryDate { get; set; } = string.Empty;

        [Required(ErrorMessage = "CVV is required")]
        [StringLength(4, MinimumLength = 3)]
        [Display(Name = "CVV")]
        public string Cvv { get; set; } = string.Empty;

        // Terms agreement
        [Range(typeof(bool), "true", "true",
            ErrorMessage = "You must agree to the terms and conditions")]
        [Display(Name = "I agree to the Terms and Conditions")]
        public bool AgreeToTerms { get; set; }

        // Order notes
        [StringLength(500)]
        [Display(Name = "Order Notes (Optional)")]
        public string OrderNotes { get; set; } = string.Empty;

        // Helper methods
        public string GetShippingAddress()
        {
            if (!string.IsNullOrEmpty(ShippingAddress))
                return ShippingAddress;

            return $"{StreetAddress}, {City}, {State} {PostalCode}, {Country}";
        }

        public string GetBillingAddress()
        {
            if (!string.IsNullOrEmpty(BillingAddress))
                return BillingAddress;

            if (SameAsShipping)
                return GetShippingAddress();

            return $"{BillingStreetAddress}, {BillingCity}, {BillingState} {BillingPostalCode}, {Country}";
        }
    }
}