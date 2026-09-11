using System;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace SmartGear_Online.Models
{
    // ===================================================
    // ORDER STATUS ENUM
    // Replaces the string-based Order.Status (nvarchar(50))
    // so status values are compile-time safe and cannot
    // contain typos like "Shippedd".
    // ===================================================
    public enum OrderStatus
    {
        [Display(Name = "Pending")]
        Pending = 0,

        [Display(Name = "Confirmed")]
        Confirmed = 1,

        [Display(Name = "In Production")]
        InProduction = 2,

        [Display(Name = "Shipped")]
        Shipped = 3,

        [Display(Name = "Delivered")]
        Delivered = 4,

        [Display(Name = "Cancelled")]
        Cancelled = 5
    }

    /// <summary>
    /// Helpers for working with <see cref="OrderStatus"/>.
    /// </summary>
    public static class OrderStatusExtensions
    {
        /// <summary>
        /// Human-readable label for a status, e.g. "Pending" or "In Production".
        /// Matches the string values historically stored in the database.
        /// </summary>
        public static string ToDisplayString(this OrderStatus status)
        {
            var member = typeof(OrderStatus).GetMember(status.ToString());
            if (member.Length == 0) return status.ToString();

            var display = member[0].GetCustomAttribute<DisplayAttribute>();
            return display?.Name ?? status.ToString();
        }

        /// <summary>
        /// Parses a status from its display name (or enum name), tolerating
        /// leading/trailing whitespace. Used by controllers that receive the
        /// status from the front-end as a string, and by the EF value converter.
        /// </summary>
        public static bool TryParseDisplayName(string? value, out OrderStatus status)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                foreach (var candidate in Enum.GetValues<OrderStatus>())
                {
                    if (string.Equals(candidate.ToDisplayString(), value, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(candidate.ToString(), value, StringComparison.OrdinalIgnoreCase))
                    {
                        status = candidate;
                        return true;
                    }
                }
            }

            status = default;
            return false;
        }
    }
}