using SmartGear_Online.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartGear_Online.Models
{
    /// Question 5: MVC Model for OrderItem
    /// Represents line items in an order
    public class OrderItem
    {
        [Key]
        public int OrderItemId { get; set; }

        [Required]
        [ForeignKey("Order")]
        public int OrderId { get; set; }

        [Required]
        [ForeignKey("Product")]
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Quantity is required")]
        [Range(1, 1000, ErrorMessage = "Quantity must be between 1 & 1000")]
        public int Quantity { get; set; }

        [Required]
        [Range(0.01, 10000)]
        [DataType(DataType.Currency)]
        [Display(Name = "Unit Price ($)")]
        public decimal UnitPrice { get; set; }

        // Navigation properties
        public virtual Order Order { get; set; }
        public virtual Product Product { get; set; }

        // ===================================================
        // BUSINESS LOGIC
        // ===================================================

        /// <summary>
        /// Calculate line total (quantity × unit price)
        /// </summary>
        public decimal GetLineTotal()
        {
            return Quantity * UnitPrice;
        }
    }
}