using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartGear_Online.Models
{
    /// <summary>
    /// QUESTION 3 & 5: CartItem Model
    /// Represents an item in the user's shopping cart
    /// Stored in session (not persistent database)
    /// </summary>
    public class CartItem
    {
        [Key]
        public int CartItemId { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        [StringLength(200)]
        public string ProductName { get; set; } = string.Empty;

        [Required]
        [Range(0.01, 10000)]
        [DataType(DataType.Currency)]
        public decimal Price { get; set; }

        [Required]
        [Range(1, 999)]
        public int Quantity { get; set; }

        [StringLength(500)]
        public string ImageUrl { get; set; } = string.Empty;

        // Customization properties (for customized products)
        public int? CustomizationId { get; set; }

        [StringLength(50)]
        public string Color { get; set; } = string.Empty;

        [StringLength(500)]
        public string LogoImageUrl { get; set; } = string.Empty;

        [StringLength(100)]
        public string CustomText { get; set; } = string.Empty;

        // Additional properties
        public DateTime AddedDate { get; set; } = DateTime.UtcNow;

        public string SessionId { get; set; } = string.Empty;

        // Calculated property (not stored in database)
        [NotMapped]
        public decimal LineTotal => Quantity * Price;

        // Business logic methods
        public void UpdateQuantity(int newQuantity)
        {
            if (newQuantity < 1)
                throw new ArgumentException("Quantity must be at least 1");
            if (newQuantity > 999)
                throw new ArgumentException("Quantity cannot exceed 999");

            Quantity = newQuantity;
        }

        public void IncrementQuantity()
        {
            if (Quantity < 999)
                Quantity++;
        }

        public void DecrementQuantity()
        {
            if (Quantity > 1)
                Quantity--;
        }

        public string GetDisplayName()
        {
            var displayName = ProductName;
            if (!string.IsNullOrEmpty(Color))
                displayName += $" ({Color})";
            if (!string.IsNullOrEmpty(CustomText))
                displayName += $" - {CustomText}";
            return displayName;
        }
    }
}