using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SmartGear_Online.Models;
using System;

namespace SmartGear_Online.Data
{
    /// Question 6: DbContext for Entity Framework Core
    /// QUESTION 10: DB CONTEXT WITH IDENTITY
    /// Manages database connection & entities
    /// Maps C# classes to database tables
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Define DbSets (tables)
        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Customization> Customizations { get; set; }
        public DbSet<Inventory> Inventory { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure relationships
            builder.Entity<Order>()
                .HasOne(o => o.Customer)
                .WithMany()
                .HasForeignKey(o => o.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<OrderItem>()
                .HasOne(oi => oi.Order)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<OrderItem>()
                .HasOne(oi => oi.Product)
                .WithMany()
                .HasForeignKey(oi => oi.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Customization>()
                .HasOne(c => c.Product)
                .WithMany()
                .HasForeignKey(c => c.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Inventory>()
                .HasOne(i => i.Product)
                .WithMany()
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure decimal precision to fix warnings
            builder.Entity<Order>()
                .Property(o => o.TotalPrice)
                .HasPrecision(18, 2);

            builder.Entity<OrderItem>()
                .Property(oi => oi.UnitPrice)
                .HasPrecision(18, 2);

            builder.Entity<Product>()
                .Property(p => p.Price)
                .HasPrecision(18, 2);

            // Create indexes for performance
            builder.Entity<Product>()
                .HasIndex(p => p.ProductName)
                .IsUnique();

            builder.Entity<Product>()
                .HasIndex(p => p.Category);

            builder.Entity<Order>()
                .HasIndex(o => o.CustomerId);

            builder.Entity<Order>()
                .HasIndex(o => o.Status);

            builder.Entity<Order>()
                .HasIndex(o => o.OrderDate);

            // REMOVED: Custom Identity table name mappings
            // Using default table names (AspNetRoles, AspNetUsers, etc.) for compatibility

            // Seed initial data
            SeedData(builder);
        }

        private void SeedData(ModelBuilder builder)
        {
            // Seed categories
            builder.Entity<Category>().HasData(
                new Category
                {
                    CategoryId = 1,
                    CategoryName = "Jerseys",
                    Description = "Team jerseys & uniforms",
                    CreatedDate = DateTime.UtcNow
                },
                new Category
                {
                    CategoryId = 2,
                    CategoryName = "Shoes",
                    Description = "Athletic & sports shoes",
                    CreatedDate = DateTime.UtcNow
                },
                new Category
                {
                    CategoryId = 3,
                    CategoryName = "Gear",
                    Description = "Sports equipment & accessories",
                    CreatedDate = DateTime.UtcNow
                },
                new Category
                {
                    CategoryId = 4,
                    CategoryName = "Hats",
                    Description = "Caps, beanies & hats",
                    CreatedDate = DateTime.UtcNow
                }
            );

            // Seed sample products
            builder.Entity<Product>().HasData(
                new Product
                {
                    ProductId = 1,
                    ProductName = "Nike Custom Jersey 2024",
                    Category = "Jerseys",
                    Price = 89.99m,
                    Description = "High-quality custom team jersey with name & number",
                    ImageUrl = "/images/jersey1.jpg",
                    QuantityInStock = 50,
                    ReorderLevel = 10,
                    CreatedDate = DateTime.UtcNow,
                    UpdatedDate = DateTime.UtcNow,
                    IsActive = true
                },
                new Product
                {
                    ProductId = 2,
                    ProductName = "Adidas Soccer Shoe",
                    Category = "Shoes",
                    Price = 129.99m,
                    Description = "Professional soccer shoes with customizable colors",
                    ImageUrl = "/images/shoe1.jpg",
                    QuantityInStock = 35,
                    ReorderLevel = 10,
                    CreatedDate = DateTime.UtcNow,
                    UpdatedDate = DateTime.UtcNow,
                    IsActive = true
                }
            );
        }
    }
}