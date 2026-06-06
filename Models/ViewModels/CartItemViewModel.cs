using System;

namespace SmartGear_Online.Models.ViewModels
{
    /// <summary>
    /// ViewModel for displaying cart items in views
    /// Used by Cart/Index.cshtml
    /// </summary>
    public class CartItemViewModel
    {
        public int CartItemId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public string CustomText { get; set; } = string.Empty;
        public string LogoImageUrl { get; set; } = string.Empty;
        public int? CustomizationId { get; set; }

        // Calculated property
        public decimal LineTotal => Quantity * UnitPrice;

        // Display properties
        public string DisplayName => GetDisplayName();
        public string DisplayPrice => $"${UnitPrice:F2}";
        public string DisplayLineTotal => $"${LineTotal:F2}";

        private string GetDisplayName()
        {
            var name = ProductName;
            if (!string.IsNullOrEmpty(Color))
                name += $" [{Color}]";
            if (!string.IsNullOrEmpty(CustomText))
                name += $" - \"{CustomText}\"";
            return name;
        }
    }
}