using Microsoft.EntityFrameworkCore;
using SmartGear_Online.Models;
using Xunit;

namespace SmartGearOnline.Tests;

/// <summary>
/// Covers the transactional order pipeline: PlaceOrderAsync (validation,
/// totals, payment, single-transaction stock decrement) and UpdateStatusAsync
/// (transition guard + cancel-time stock restore).
/// </summary>
public class OrderProcessingTests
{
    [Fact]
    public async Task PlaceOrder_CreatesOrderPersistsTotalsAndDecrementsStock()
    {
        using var context = TestDb.CreateContext();
        var service = TestDb.CreateService(context);
        TestDb.SeedCustomer(context);
        var product = TestDb.SeedProduct(context, 9010, price: 100, stock: 10);

        var cart = new ShoppingCart();
        cart.AddItem(TestDb.CartItemFor(product, 2));

        var orderId = await service.PlaceOrderAsync("customer-1", cart, TestDb.ValidCheckout());

        var order = await context.Orders.AsNoTracking()
            .Include(o => o.OrderItems)
            .SingleAsync(o => o.OrderId == orderId);

        // Subtotal 200 + tax 16 + free standard shipping = 216.
        Assert.Equal(216, order.TotalPrice);
        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.Null(order.DiscountCode);
        var item = Assert.Single(order.OrderItems);
        Assert.Equal(product.ProductId, item.ProductId);
        Assert.Equal(2, item.Quantity);
        Assert.Equal(100, item.UnitPrice);

        // Stock decremented through the atomic update.
        var stock = (await context.Products.AsNoTracking().SingleAsync(p => p.ProductId == product.ProductId)).QuantityInStock;
        Assert.Equal(8, stock);
    }

    [Fact]
    public async Task PlaceOrder_WithDiscount_PersistsCodeAndAmount()
    {
        using var context = TestDb.CreateContext();
        var service = TestDb.CreateService(context);
        TestDb.SeedCustomer(context);
        var product = TestDb.SeedProduct(context, 9011, price: 100, stock: 10);

        var cart = new ShoppingCart();
        cart.AddItem(TestDb.CartItemFor(product, 2));
        cart.DiscountCode = "welcome10";

        var orderId = await service.PlaceOrderAsync("customer-1", cart, TestDb.ValidCheckout());

        var order = await context.Orders.AsNoTracking().SingleAsync(o => o.OrderId == orderId);
        Assert.Equal("WELCOME10", order.DiscountCode);
        Assert.Equal(20, order.DiscountAmount);
        Assert.Equal(196, order.TotalPrice); // 216 - 20
    }

    [Fact]
    public async Task PlaceOrder_Oversell_ThrowsAndPersistsNothing()
    {
        using var context = TestDb.CreateContext();
        var service = TestDb.CreateService(context);
        var product = TestDb.SeedProduct(context, 9012, price: 100, stock: 10);

        var cart = new ShoppingCart();
        cart.AddItem(TestDb.CartItemFor(product, 15));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.PlaceOrderAsync("customer-1", cart, TestDb.ValidCheckout()));

        Assert.Equal(0, await context.Orders.CountAsync());
        var stock = (await context.Products.AsNoTracking().SingleAsync(p => p.ProductId == product.ProductId)).QuantityInStock;
        Assert.Equal(10, stock);
    }

    [Fact]
    public async Task PlaceOrder_InactiveProduct_Throws()
    {
        using var context = TestDb.CreateContext();
        var service = TestDb.CreateService(context);
        var product = TestDb.SeedProduct(context, 9013, price: 100, stock: 10, active: false);

        var cart = new ShoppingCart();
        cart.AddItem(TestDb.CartItemFor(product, 1));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.PlaceOrderAsync("customer-1", cart, TestDb.ValidCheckout()));
        Assert.Equal(0, await context.Orders.CountAsync());
        Assert.Equal(0, await context.OrderItems.CountAsync());
    }

    [Fact]
    public async Task PlaceOrder_EmptyCart_Throws()
    {
        using var context = TestDb.CreateContext();
        var service = TestDb.CreateService(context);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.PlaceOrderAsync("customer-1", new ShoppingCart(), TestDb.ValidCheckout()));
    }

    [Fact]
    public async Task UpdateStatus_ForwardTransition_Succeeds()
    {
        using var context = TestDb.CreateContext();
        var service = TestDb.CreateService(context);
        var product = TestDb.SeedProduct(context, 9020, price: 100, stock: 5);
        var order = TestDb.CreateOrder(context, product, quantity: 2, OrderStatus.Pending);

        var result = await service.UpdateStatusAsync(order.OrderId, OrderStatus.Confirmed, "admin-1");

        Assert.True(result.Success);
        var saved = await context.Orders.AsNoTracking().SingleAsync(o => o.OrderId == order.OrderId);
        Assert.Equal(OrderStatus.Confirmed, saved.Status);
    }

    [Fact]
    public async Task UpdateStatus_BackwardTransition_Fails()
    {
        using var context = TestDb.CreateContext();
        var service = TestDb.CreateService(context);
        var product = TestDb.SeedProduct(context, 9021, price: 100, stock: 5);
        var order = TestDb.CreateOrder(context, product, quantity: 2, OrderStatus.Confirmed);

        var result = await service.UpdateStatusAsync(order.OrderId, OrderStatus.Pending, "admin-1");

        Assert.False(result.Success);
        var saved = await context.Orders.AsNoTracking().SingleAsync(o => o.OrderId == order.OrderId);
        Assert.Equal(OrderStatus.Confirmed, saved.Status);
    }

    [Fact]
    public async Task UpdateStatus_SameStatus_IsIdempotentSuccess()
    {
        using var context = TestDb.CreateContext();
        var service = TestDb.CreateService(context);
        var product = TestDb.SeedProduct(context, 9022, price: 100, stock: 5);
        var order = TestDb.CreateOrder(context, product, quantity: 2, OrderStatus.Pending);

        var result = await service.UpdateStatusAsync(order.OrderId, OrderStatus.Pending, "admin-1");

        Assert.True(result.Success);
    }

    [Fact]
    public async Task UpdateStatus_Cancel_RestoresStockInSameTransaction()
    {
        using var context = TestDb.CreateContext();
        var service = TestDb.CreateService(context);
        var product = TestDb.SeedProduct(context, 9023, price: 100, stock: 5);
        var order = TestDb.CreateOrder(context, product, quantity: 2, OrderStatus.Pending);

        var result = await service.UpdateStatusAsync(order.OrderId, OrderStatus.Cancelled, "admin-1");

        Assert.True(result.Success);
        var stock = (await context.Products.AsNoTracking().SingleAsync(p => p.ProductId == product.ProductId)).QuantityInStock;
        Assert.Equal(7, stock); // 5 + 2 restored
    }

    [Fact]
    public async Task UpdateStatus_CancelledOrder_CannotChangeAgain()
    {
        using var context = TestDb.CreateContext();
        var service = TestDb.CreateService(context);
        var product = TestDb.SeedProduct(context, 9024, price: 100, stock: 5);
        var order = TestDb.CreateOrder(context, product, quantity: 2, OrderStatus.Pending);

        await service.UpdateStatusAsync(order.OrderId, OrderStatus.Cancelled, "admin-1");
        var result = await service.UpdateStatusAsync(order.OrderId, OrderStatus.Shipped, "admin-1");

        Assert.False(result.Success);
    }

    [Fact]
    public async Task UpdateStatus_DeliveredIsTerminal()
    {
        using var context = TestDb.CreateContext();
        var service = TestDb.CreateService(context);
        var product = TestDb.SeedProduct(context, 9025, price: 100, stock: 5);
        var order = TestDb.CreateOrder(context, product, quantity: 2, OrderStatus.Delivered);

        var result = await service.UpdateStatusAsync(order.OrderId, OrderStatus.Cancelled, "admin-1");

        Assert.False(result.Success);
    }

    [Fact]
    public async Task UpdateStatus_UnknownOrder_TellsCaller()
    {
        using var context = TestDb.CreateContext();
        var service = TestDb.CreateService(context);

        var result = await service.UpdateStatusAsync(999999, OrderStatus.Confirmed, "admin-1");

        Assert.False(result.Success);
    }
}