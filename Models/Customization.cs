using SmartGear_Online.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartGear_Online.Models
{
    /// Question 5: MVC Model for Customization
    /// Represents custom options applied to products
    public class Customization
    {
        [Key]
        public int CustomizationId { get; set; }

        [Required]
        [ForeignKey("Product")]
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Color is required")]
        [StringLength(50)]
        public string Color { get; set; }

        [StringLength(500)]
        [Display(Name = "Logo Image URL")]
        public string LogoImageUrl { get; set; }

        [StringLength(100)]
        [Display(Name = "Custom Text")]
        public string CustomText { get; set; }

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Navigation property
        public virtual Product Product { get; set; }

        // ===================================================
        // VALIDATION LOGIC
        // ===================================================

        public bool IsValidColor(string colorCode)
        {
            // Validate color format (hex code or color name)
            var validColors = new[] { "Red", "Blue", "Green", "Yellow", "White",
                                     "Black", "Orange", "Purple" };
            return validColors.Contains(colorCode);
        }
    }
}