using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SmartGear_Online.Data;
using SmartGear_Online.Models;
using SmartGear_Online.Models.ViewModels;
using SmartGear_Online.Services;

namespace SmartGearOnline.Tests;

/// <summary>
/// Shared test plumbing. Each test gets a fresh in-memory SQLite database
/// (the file-less provider keeps a single EF execution strategy, supports
/// transactions and ExecuteUpdate/ExecuteDelete, unlike the EF InMemory
/// provider which intentionally lacks them).
/// </summary>
public static class TestDb
{
    public static ApplicationDbContext CreateContext()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    public static OrderService CreateService(ApplicationDbContext context) =>
        new OrderService(
            NullLogger<OrderService>.Instance,
            Options.Create(new OrderSettings()),
            Options.Create(new DiscountSettings
            {
                Codes = new List<DiscountCode>
                {
                    new() { Code = "WELCOME10", Type = "Percentage", Value = 10, IsActive = true },
                    new() { Code = "SAVE20", Type = "Percentage", Value = 20, IsActive = true },
                    new() { Code = "FREESHIP", Type = "FreeShipping", Value = 0, IsActive = true },
                    new() { Code = "FLAT25", Type = "FixedAmount", Value = 25, IsActive = true },
                    new() { Code = "RETIRED", Type = "Percentage", Value = 50, IsActive = false },
                    new() { Code = "OUTDATED", Type = "Percentage", Value = 50, IsActive = true, ExpiryDate = DateTime.UtcNow.AddDays(-1) }
                }
            }),
            context);

    /// <summary>Seeds a product with an explicit high ID to avoid colliding
    /// with the seed data in OnModelCreating.</summary>
    public static Product SeedProduct(
        ApplicationDbContext context,
        int id,
        decimal price,
        int stock,
        bool active = true)
    {
        var product = new Product
        {
            ProductId = id,
            ProductName = $"Test Product {id}",
            Category = "Test",
            Price = price,
            Description = $"Test description for product {id}.",
            ImageUrl = "https://example.com/img.png",
            QuantityInStock = stock,
            ReorderLevel = 3,
            IsActive = active
        };
        context.Products.Add(product);
        context.SaveChanges();
        return product;
    }

    public static CartItem CartItemFor(Product product, int quantity) =>
        new()
        {
            ProductId = product.ProductId,
            ProductName = product.ProductName,
            Price = product.Price,
            Quantity = quantity
        };

    public static CheckoutViewModel ValidCheckout() =>
        new()
        {
            FullName = "Test User",
            Email = "test@example.com",
            PhoneNumber = "5551234567",
            StreetAddress = "1 Main Street",
            City = "Springfield",
            State = "IL",
            PostalCode = "62701",
            Country = "United States",
            ShippingMethod = "Standard",
            CardNumberLast4 = "4242",
            ExpiryDate = "12/30"
        };

    /// <summary>Seeds the ApplicationUser referenced by Order.CustomerId.
    /// The SQLite provider enables foreign-key enforcement, so an order
    /// cannot reference a CustomerId that has no AspNetUsers row.</summary>
    public static void SeedCustomer(ApplicationDbContext context)
    {
        if (context.Users.Any(u => u.Id == "customer-1"))
            return;

        context.Users.Add(new ApplicationUser
        {
            Id = "customer-1",
            UserName = "customer-1",
            NormalizedUserName = "CUSTOMER-1",
            Email = "customer-1@example.com",
            NormalizedEmail = "CUSTOMER-1@EXAMPLE.COM",
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            FullName = "Test Customer"
        });
        context.SaveChanges();
    }

    /// <summary>Creates an order directly in the database (used by the
    /// status-transition tests, which operate on existing orders).</summary>
    public static Order CreateOrder(
        ApplicationDbContext context,
        Product product,
        int quantity,
        OrderStatus status)
    {
        SeedCustomer(context);

        var order = new Order
        {
            CustomerId = "customer-1",
            Status = status,
            TotalPrice = product.Price * quantity,
            ShippingAddress = "1 Main Street, Springfield, IL 62701, United States",
            ShippingMethod = "Standard",
            UpdatedDate = DateTime.UtcNow
        };
        context.Orders.Add(order);
        context.SaveChanges();

        context.OrderItems.Add(new OrderItem
        {
            OrderId = order.OrderId,
            ProductId = product.ProductId,
            Quantity = quantity,
            UnitPrice = product.Price
        });
        context.SaveChanges();

        return order;
    }
}