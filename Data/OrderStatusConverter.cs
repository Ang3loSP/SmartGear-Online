using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SmartGear_Online.Models;

namespace SmartGear_Online.Data
{
    /// <summary>
    /// Converts <see cref="OrderStatus"/> to/from the string values stored in
    /// the database ("Pending", "Confirmed", "In Production", ...).
    ///
    /// Choosing a string conversion (rather than an integer) keeps the column
    /// human-readable in SQL and requires no data migration for existing rows.
    /// </summary>
    public static class OrderStatusConverter
    {
        public static readonly ValueConverter<OrderStatus, string> Instance = new(
            v => v.ToDisplayString(),
            v => ParseValue(v));

        private static OrderStatus ParseValue(string? value) =>
            OrderStatusExtensions.TryParseDisplayName(value, out var status) ? status : OrderStatus.Pending;
    }
}