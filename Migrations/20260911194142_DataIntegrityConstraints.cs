using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartGear_Online.Migrations
{
    /// <inheritdoc />
    public partial class DataIntegrityConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Inventory");

            migrationBuilder.AlterColumn<string>(
                name: "ImageUrl",
                table: "Products",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "TrackingNumber",
                table: "Orders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAmount",
                table: "Orders",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "DiscountCode",
                table: "Orders",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ProfilePictureUrl",
                table: "AspNetUsers",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 1,
                column: "Description",
                value: "Team jerseys & uniforms");

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 2,
                column: "Description",
                value: "Athletic & sports shoes");

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 3,
                column: "Description",
                value: "Sports equipment & accessories");

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 4,
                column: "Description",
                value: "Caps, beanies & hats");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "ProductId",
                keyValue: 1,
                column: "Description",
                value: "High-quality custom team jersey with name & number");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Products_Price_Positive",
                table: "Products",
                sql: "[Price] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Products_QuantityInStock_NonNegative",
                table: "Products",
                sql: "[QuantityInStock] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Products_ReorderLevel_NonNegative",
                table: "Products",
                sql: "[ReorderLevel] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Orders_TotalPrice_NonNegative",
                table: "Orders",
                sql: "[TotalPrice] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_OrderItems_Quantity_Positive",
                table: "OrderItems",
                sql: "[Quantity] >= 1");

            migrationBuilder.AddCheckConstraint(
                name: "CK_OrderItems_UnitPrice_Positive",
                table: "OrderItems",
                sql: "[UnitPrice] > 0");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_CategoryName",
                table: "Categories",
                column: "CategoryName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Products_Price_Positive",
                table: "Products");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Products_QuantityInStock_NonNegative",
                table: "Products");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Products_ReorderLevel_NonNegative",
                table: "Products");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Orders_TotalPrice_NonNegative",
                table: "Orders");

            migrationBuilder.DropCheckConstraint(
                name: "CK_OrderItems_Quantity_Positive",
                table: "OrderItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_OrderItems_UnitPrice_Positive",
                table: "OrderItems");

            migrationBuilder.DropIndex(
                name: "IX_Categories_CategoryName",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "DiscountAmount",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DiscountCode",
                table: "Orders");

            migrationBuilder.AlterColumn<string>(
                name: "ImageUrl",
                table: "Products",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "TrackingNumber",
                table: "Orders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ProfilePictureUrl",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.CreateTable(
                name: "Inventory",
                columns: table => new
                {
                    InventoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    LastRestockedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    QuantityInStock = table.Column<int>(type: "int", nullable: false),
                    ReorderLevel = table.Column<int>(type: "int", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inventory", x => x.InventoryId);
                    table.ForeignKey(
                        name: "FK_Inventory_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "ProductId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 1,
                column: "Description",
                value: "Team jerseys &amp; uniforms");

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 2,
                column: "Description",
                value: "Athletic &amp; sports shoes");

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 3,
                column: "Description",
                value: "Sports equipment &amp; accessories");

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 4,
                column: "Description",
                value: "Caps, beanies &amp; hats");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "ProductId",
                keyValue: 1,
                column: "Description",
                value: "High-quality custom team jersey with name &amp; number");

            migrationBuilder.CreateIndex(
                name: "IX_Inventory_ProductId",
                table: "Inventory",
                column: "ProductId");
        }
    }
}
