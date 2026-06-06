using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartGear_Online.Models
{
    /// Question 5: MVC Model for Category
    public class Category
    {
        [Key]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Category name is required")]
        [StringLength(100, MinimumLength = 2,
            ErrorMessage = "Category name must be between 2 & 100 characters")]
        [Display(Name = "Category Name")]
        public string CategoryName { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Description")]
        public string Description { get; set; } = string.Empty;

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}