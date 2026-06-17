using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SmartGear_Online.Models;
using System;

namespace SmartGear_Online.Data
{
    /// Question 6: DbContext for Entity Framework Core
    /// QUESTION 10: DB CONTEXT WITH IDENTITY
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        // Static seed date — must never be DateTime.UtcNow.
        // EF compares seed values on every migration run; a dynamic date
        // always looks like a change &amp; causes duplicate-key errors.
        private static readonly DateTime SeedDate =
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Customization> Customizations { get; set; }
        public DbSet<Inventory> Inventory { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

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

            builder.Entity<Order>()
                .Property(o => o.TotalPrice)
                .HasPrecision(18, 2);

            builder.Entity<OrderItem>()
                .Property(oi => oi.UnitPrice)
                .HasPrecision(18, 2);

            builder.Entity<Product>()
                .Property(p => p.Price)
                .HasPrecision(18, 2);

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

            SeedData(builder);
        }

        private void SeedData(ModelBuilder builder)
        {
            builder.Entity<Category>().HasData(
                new Category
                {
                    CategoryId = 1,
                    CategoryName = "Jerseys",
                    Description = "Team jerseys &amp; uniforms",
                    CreatedDate = SeedDate
                },
                new Category
                {
                    CategoryId = 2,
                    CategoryName = "Shoes",
                    Description = "Athletic &amp; sports shoes",
                    CreatedDate = SeedDate
                },
                new Category
                {
                    CategoryId = 3,
                    CategoryName = "Gear",
                    Description = "Sports equipment &amp; accessories",
                    CreatedDate = SeedDate
                },
                new Category
                {
                    CategoryId = 4,
                    CategoryName = "Hats",
                    Description = "Caps, beanies &amp; hats",
                    CreatedDate = SeedDate
                }
            );

            builder.Entity<Product>().HasData(
                new Product
                {
                    ProductId = 1,
                    ProductName = "Nike Custom Jersey 2024",
                    Category = "Jerseys",
                    Price = 89.99m,
                    Description = "High-quality custom team jersey with name &amp; number",
                    ImageUrl = "/images/products/jersey1.jpg",
                    QuantityInStock = 50,
                    ReorderLevel = 10,
                    CreatedDate = SeedDate,
                    UpdatedDate = SeedDate,
                    IsActive = true
                },
                new Product
                {
                    ProductId = 2,
                    ProductName = "Adidas Soccer Shoe",
                    Category = "Shoes",
                    Price = 129.99m,
                    Description = "Professional soccer shoes with customizable colors",
                    ImageUrl = "/images/products/shoe1.jpg",
                    QuantityInStock = 35,
                    ReorderLevel = 10,
                    CreatedDate = SeedDate,
                    UpdatedDate = SeedDate,
                    IsActive = true
                }
            );
        }
    }
}
