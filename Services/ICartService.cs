using SmartGear_Online.Models;

namespace SmartGear_Online.Services
{
    /// <summary>
    /// Access to the session-backed shopping cart shared across controllers.
    /// </summary>
    public interface ICartService
    {
        /// <summary>
        /// Returns the current cart from session, creating and persisting an
        /// empty one on first access.
        /// </summary>
        ShoppingCart GetCart();

        /// <summary>
        /// Persists the given cart back to session.
        /// </summary>
        void SaveCart(ShoppingCart cart);

        /// <summary>
        /// Removes the cart from session entirely (e.g. after a successful
        /// checkout).
        /// </summary>
        void Clear();

        /// <summary>
        /// Generates a collision-free CartItemId for a new cart line.
        /// </summary>
        int GenerateCartItemId();
    }
}