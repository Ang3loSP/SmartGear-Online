using System;

namespace SmartGear_Online.Models
{
    /// <summary>
    /// Order-related business rules loaded from the "OrderSettings"
    /// configuration section (see appsettings.json).
    /// </summary>
    public class OrderSettings
    {
        public decimal? TaxRate { get; set; } = 0.08m;
        public decimal? FreeShippingThreshold { get; set; } = 50;
        public decimal? MaxOrderValue { get; set; } = 10000;
        public int? MaxItemsPerOrder { get; set; } = 50;
    }
}