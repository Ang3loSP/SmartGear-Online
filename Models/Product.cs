using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartGear_Online.Models
{
    /// Question 5: MVC Model for Product
    /// Represents product data & contains validation rules
    /// Database table: Products
    public class Product
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Product name is required")]
        [StringLength(200, MinimumLength = 3,
            ErrorMessage = "Product name must be between 3 & 200 characters")]
        [Display(Name = "Product Name")]
        public string ProductName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Category is required")]
        [StringLength(50)]
        public string Category { get; set; } = string.Empty;

        [Required(ErrorMessage = "Price is required")]
        [Range(0.01, 10000,
            ErrorMessage = "Price must be between $0.01 & $10,000")]
        [DataType(DataType.Currency)]
        [Display(Name = "Price ($)")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Description is required")]
        [StringLength(1000, MinimumLength = 10,
            ErrorMessage = "Description must be between 10 & 1000 characters")]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Url(ErrorMessage = "Please enter a valid image URL")]
        [Display(Name = "Image URL")]
        public string ImageUrl { get; set; } = string.Empty;

        [Required(ErrorMessage = "Quantity in stock is required")]
        [Range(0, 100000, ErrorMessage = "Quantity must be between 0 & 100,000")]
        [Display(Name = "Quantity in Stock")]
        public int QuantityInStock { get; set; }

        [Display(Name = "Reorder Level")]
        [Range(1, 1000, ErrorMessage = "Reorder level must be between 1 & 1000")]
        public int ReorderLevel { get; set; } = 10;

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [Display(Name = "Updated Date")]
        public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        // ===================================================
        // BUSINESS LOGIC & HELPER METHODS
        // ===================================================

        /// <summary>
        /// Question 5: Business Logic - Calculate discounted price
        /// </summary>
        public decimal GetDiscountedPrice(decimal discountPercentage)
        {
            if (discountPercentage < 0 || discountPercentage > 100)
                throw new ArgumentException("Discount must be between 0 & 100");

            decimal discountAmount = Price * (discountPercentage / 100);
            return Price - discountAmount;
        }

        /// <summary>
        /// Question 5: Business Logic - Check if product needs reordering
        /// </summary>
        public bool NeedsReordering()
        {
            return QuantityInStock < ReorderLevel;
        }

        /// <summary>
        /// Question 5: Business Logic - Check if product is in stock
        /// </summary>
        public bool IsInStock()
        {
            return QuantityInStock > 0;
        }

        /// <summary>
        /// Question 5: Business Logic - Check if can fulfill order
        /// </summary>
        public bool CanFulfillOrder(int quantityRequested)
        {
            return quantityRequested > 0 & quantityRequested <= QuantityInStock;
        }

        /// <summary>
        /// Reduce inventory when order is placed
        /// </summary>
        public void ReduceInventory(int quantity)
        {
            if (quantity <= 0)
                throw new ArgumentException("Quantity must be positive");

            if (!CanFulfillOrder(quantity))
                throw new InvalidOperationException(
                    "Insufficient inventory for this order");

            QuantityInStock -= quantity;
            UpdatedDate = DateTime.UtcNow;
        }

        /// <summary>
        /// Increase inventory when stock is replenished
        /// </summary>
        public void ReplenishInventory(int quantity)
        {
            if (quantity <= 0)
                throw new ArgumentException("Quantity must be positive");

            QuantityInStock += quantity;
            UpdatedDate = DateTime.UtcNow;
        }
    }
}