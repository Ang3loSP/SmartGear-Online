using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartGear_Online.Models
{
    /// Question 6: Inventory Model
    /// Tracks product stock levels
    public class Inventory
    {
        [Key]
        public int InventoryId { get; set; }

        [Required]
        [ForeignKey("Product")]
        public int ProductId { get; set; }

        [Required]
        [Range(0, 100000)]
        public int QuantityInStock { get; set; }

        [Required]
        [Range(1, 1000)]
        public int ReorderLevel { get; set; } = 10;

        [Display(Name = "Last Restocked")]
        public DateTime LastRestockedDate { get; set; } = DateTime.UtcNow;

        [Display(Name = "Updated")]
        public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;

        // Navigation property
        public virtual Product? Product { get; set; }

        public bool NeedsReordering()
        {
            return QuantityInStock <= ReorderLevel;
        }
    }
}