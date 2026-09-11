using System;
using System.Collections.Generic;

namespace SmartGear_Online.Models
{
    /// <summary>
    /// Discount codes loaded from the "Discounts" configuration section
    /// (see appsettings.json). Codes can be changed at deploy time without
    /// editing code — the OrderService builds its lookup table from here.
    /// </summary>
    public class DiscountSettings
    {
        public List<DiscountCode> Codes { get; set; } = new List<DiscountCode>();
    }

    public class DiscountCode
    {
        public string Code { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // "Percentage", "FixedAmount", "FreeShipping"
        public decimal Value { get; set; }
        public bool IsActive { get; set; }
        public DateTime? ExpiryDate { get; set; }
    }
}