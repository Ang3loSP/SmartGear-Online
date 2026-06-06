using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace SmartGear_Online.Models
{
    /// Question 5: MVC Model for Order
    /// Represents customer orders with validation
    public class Order
    {
        [Key]
        public int OrderId { get; set; }

        [Required]
        [ForeignKey("Customer")]
        public string CustomerId { get; set; } = string.Empty;

        [Display(Name = "Order Date")]
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        [Display(Name = "Updated Date")]
        public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;

        [Required(ErrorMessage = "Order status is required")]
        [StringLength(50)]
        public string Status { get; set; } = "Pending";

        [Required(ErrorMessage = "Total price is required")]
        [Range(0.01, 999999, ErrorMessage = "Total must be greater than 0")]
        [DataType(DataType.Currency)]
        [Display(Name = "Total Price ($)")]
        public decimal TotalPrice { get; set; }

        [Required(ErrorMessage = "Shipping address is required")]
        [StringLength(500)]
        [Display(Name = "Shipping Address")]
        public string ShippingAddress { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Billing Address")]
        public string BillingAddress { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "Shipping Method")]
        public string ShippingMethod { get; set; } = "Standard";

        [StringLength(100)]
        [Display(Name = "Tracking Number")]
        public string TrackingNumber { get; set; } = string.Empty;

        // Navigation properties
        public virtual ApplicationUser? Customer { get; set; }
        public virtual ICollection<OrderItem> OrderItems { get; set; }
            = new List<OrderItem>();

        // ===================================================
        // BUSINESS LOGIC
        // ===================================================

        /// <summary>
        /// Calculate total price from items
        /// </summary>
        public decimal CalculateTotal()
        {
            return OrderItems?.Sum(x => x.Quantity * x.UnitPrice) ?? 0;
        }

        /// <summary>
        /// Check if order can be cancelled
        /// </summary>
        public bool CanBeCancelled()
        {
            return Status == "Pending" || Status == "Confirmed";
        }

        /// <summary>
        /// Get order status display name
        /// </summary>
        public string GetStatusDisplayName()
        {
            return Status switch
            {
                "Pending" => "Order Received - Awaiting Confirmation",
                "Confirmed" => "Order Confirmed - In Production",
                "In Production" => "Items Being Customized & Assembled",
                "Shipped" => "Order Shipped - On the Way",
                "Delivered" => "Order Delivered",
                "Cancelled" => "Order Cancelled",
                _ => "Unknown Status"
            };
        }

        /// <summary>
        /// Check if order has been shipped
        /// </summary>
        public bool IsShipped()
        {
            return Status == "Shipped" || Status == "Delivered";
        }
    }
}