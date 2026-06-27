using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartGear_Online.Migrations
{
    /// <inheritdoc />
    public partial class FixSeedDates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 1,
                columns: new[] { "CreatedDate", "Description" },
                values: new object[] { new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Team jerseys &amp; uniforms" });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 2,
                columns: new[] { "CreatedDate", "Description" },
                values: new object[] { new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Athletic &amp; sports shoes" });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 3,
                columns: new[] { "CreatedDate", "Description" },
                values: new object[] { new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Sports equipment &amp; accessories" });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 4,
                columns: new[] { "CreatedDate", "Description" },
                values: new object[] { new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Caps, beanies &amp; hats" });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "ProductId",
                keyValue: 1,
                columns: new[] { "CreatedDate", "Description", "ImageUrl", "UpdatedDate" },
                values: new object[] { new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "High-quality custom team jersey with name &amp; number", "/images/products/jersey1.jpg", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "ProductId",
                keyValue: 2,
                columns: new[] { "CreatedDate", "ImageUrl", "UpdatedDate" },
                values: new object[] { new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "/images/products/shoe1.jpg", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 1,
                columns: new[] { "CreatedDate", "Description" },
                values: new object[] { new DateTime(2026, 5, 17, 21, 15, 41, 724, DateTimeKind.Utc).AddTicks(3183), "Team jerseys & uniforms" });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 2,
                columns: new[] { "CreatedDate", "Description" },
                values: new object[] { new DateTime(2026, 5, 17, 21, 15, 41, 724, DateTimeKind.Utc).AddTicks(3186), "Athletic & sports shoes" });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 3,
                columns: new[] { "CreatedDate", "Description" },
                values: new object[] { new DateTime(2026, 5, 17, 21, 15, 41, 724, DateTimeKind.Utc).AddTicks(3188), "Sports equipment & accessories" });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 4,
                columns: new[] { "CreatedDate", "Description" },
                values: new object[] { new DateTime(2026, 5, 17, 21, 15, 41, 724, DateTimeKind.Utc).AddTicks(3190), "Caps, beanies & hats" });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "ProductId",
                keyValue: 1,
                columns: new[] { "CreatedDate", "Description", "ImageUrl", "UpdatedDate" },
                values: new object[] { new DateTime(2026, 5, 17, 21, 15, 41, 724, DateTimeKind.Utc).AddTicks(3761), "High-quality custom team jersey with name & number", "/images/jersey1.jpg", new DateTime(2026, 5, 17, 21, 15, 41, 724, DateTimeKind.Utc).AddTicks(3762) });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "ProductId",
                keyValue: 2,
                columns: new[] { "CreatedDate", "ImageUrl", "UpdatedDate" },
                values: new object[] { new DateTime(2026, 5, 17, 21, 15, 41, 724, DateTimeKind.Utc).AddTicks(3767), "/images/shoe1.jpg", new DateTime(2026, 5, 17, 21, 15, 41, 724, DateTimeKind.Utc).AddTicks(3768) });
        }
    }
}
