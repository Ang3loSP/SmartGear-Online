using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartGear_Online.Models
{
    /// <summary>
    /// QUESTION 3 & 5: ShoppingCart Model
    /// Container for multiple CartItems
    /// Stored in session as a single object
    /// </summary>
    public class ShoppingCart
    {
        public List<CartItem> Items { get; set; } = new List<CartItem>();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;

        public string DiscountCode { get; set; } = string.Empty;

        public decimal DiscountAmount { get; set; }

        // Calculated properties
        public int ItemCount => Items?.Sum(i => i.Quantity) ?? 0;

        public int UniqueItemCount => Items?.Count ?? 0;

        public decimal Subtotal => Items?.Sum(i => i.LineTotal) ?? 0;

        // FIXED: Added GetTotal method for compatibility
        public decimal GetTotal()
        {
            return Subtotal;
        }

        // Business logic methods
        public void AddItem(CartItem newItem)
        {
            if (newItem == null)
                throw new ArgumentNullException(nameof(newItem));

            // Check if item already exists in cart (same product and customization)
            var existingItem = Items.FirstOrDefault(i =>
                i.ProductId == newItem.ProductId &&
                i.CustomizationId == newItem.CustomizationId);

            if (existingItem != null)
            {
                // Update quantity instead of adding duplicate
                existingItem.UpdateQuantity(existingItem.Quantity + newItem.Quantity);
            }
            else
            {
                Items.Add(newItem);
            }

            LastUpdatedAt = DateTime.UtcNow;
        }

        public void RemoveItem(int cartItemId)
        {
            var item = Items.FirstOrDefault(i => i.CartItemId == cartItemId);
            if (item != null)
            {
                Items.Remove(item);
                LastUpdatedAt = DateTime.UtcNow;
            }
        }

        public void UpdateQuantity(int cartItemId, int newQuantity)
        {
            var item = Items.FirstOrDefault(i => i.CartItemId == cartItemId);
            if (item != null)
            {
                if (newQuantity <= 0)
                {
                    RemoveItem(cartItemId);
                }
                else
                {
                    item.UpdateQuantity(newQuantity);
                    LastUpdatedAt = DateTime.UtcNow;
                }
            }
        }

        public void ClearCart()
        {
            Items.Clear();
            DiscountCode = string.Empty;
            DiscountAmount = 0;
            LastUpdatedAt = DateTime.UtcNow;
        }

        public decimal GetTotalAfterDiscount()
        {
            return Subtotal - DiscountAmount;
        }

        public bool HasItems()
        {
            return Items != null && Items.Any();
        }

        public CartItem GetItem(int productId, int? customizationId = null)
        {
            return Items.FirstOrDefault(i =>
                i.ProductId == productId &&
                i.CustomizationId == customizationId);
        }
    }
}