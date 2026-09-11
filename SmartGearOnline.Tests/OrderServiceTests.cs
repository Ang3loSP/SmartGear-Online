using SmartGear_Online.Models;
using SmartGear_Online.Services;
using Xunit;

namespace SmartGearOnline.Tests;

/// <summary>
/// Covers ApplyDiscount (pure in-memory computation) and
/// CalculateOrderTotalsAsync (database-price driven). Together these are the
/// single source of truth for every number a customer sees at checkout.
/// </summary>
public class OrderServiceTests
{
    // ==========================================================
    // ApplyDiscount
    // ==========================================================

    [Theory]
    [InlineData("WELCOME10", 200.00, 20.00)]
    [InlineData("SAVE20", 200.00, 40.00)]
    [InlineData("FLAT25", 200.00, 25.00)]
    public void ApplyDiscount_ValidPercentageAndFixed_CorrectAmount(
        string code, decimal subtotal, decimal expected)
    {
        using var context = TestDb.CreateContext();
        var service = TestDb.CreateService(context);

        var result = service.ApplyDiscount(code, subtotal);

        Assert.True(result.IsValid);
        Assert.Equal(expected, result.DiscountAmount);
    }

    [Fact]
    public void ApplyDiscount_IsCaseInsensitiveAndTrimsWhitespace()
    {
        using var context = TestDb.CreateContext();
        var service = TestDb.CreateService(context);

        var result = service.ApplyDiscount("  welcome10  ", 200);

        Assert.True(result.IsValid);
        Assert.Equal(20, result.DiscountAmount);
    }

    [Fact]
    public void ApplyDiscount_PercentageCappedAtFiveHundred()
    {
        using var context = TestDb.CreateContext();
        var service = TestDb.CreateService(context);

        var result = service.ApplyDiscount("WELCOME10", 6000);

        Assert.True(result.IsValid);
        Assert.Equal(500, result.DiscountAmount);
    }

    [Fact]
    public void ApplyDiscount_FixedAmountCappedAtSubtotal()
    {
        using var context = TestDb.CreateContext();
        var service = TestDb.CreateService(context);

        var result = service.ApplyDiscount("FLAT25", 10);

        Assert.True(result.IsValid);
        Assert.Equal(10, result.DiscountAmount);
    }

    [Fact]
    public void ApplyDiscount_FreeShipping_ReturnsZeroAmountAndType()
    {
        using var context = TestDb.CreateContext();
        var service = TestDb.CreateService(context);

        var result = service.ApplyDiscount("FREESHIP", 200);

        Assert.True(result.IsValid);
        Assert.Equal(0, result.DiscountAmount);
        Assert.Equal("FreeShipping", result.DiscountType);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("NOPE123")]
    [InlineData("RETIRED")]          // inactive code
    [InlineData("OUTDATED")]         // active but past expiry
    public void ApplyDiscount_InvalidNullOrExpired_ReturnsInvalid(string? code)
    {
        using var context = TestDb.CreateContext();
        var service = TestDb.CreateService(context);

        var result = service.ApplyDiscount(code!, 200);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void ApplyDiscount_OverlongCode_ReturnsInvalid()
    {
        using var context = TestDb.CreateContext();
        var service = TestDb.CreateService(context);

        var result = service.ApplyDiscount(new string('A', 40), 200);

        Assert.False(result.IsValid);
    }

    // ==========================================================
    // CalculateOrderTotalsAsync
    // ==========================================================

    [Fact]
    public async Task Totals_UsesCurrentDatabasePriceNotStaleCartPrice()
    {
        using var context = TestDb.CreateContext();
        var service = TestDb.CreateService(context);
        var product = TestDb.SeedProduct(context, 9001, price: 50, stock: 10);

        // The cart item snapshot pretends to be $999; the service must ignore it.
        var cartItems = new List<CartItem> { TestDb.CartItemFor(product, 2) };

        var totals = await service.CalculateOrderTotalsAsync(cartItems, "Standard");

        // DB price $50 x 2
        Assert.Equal(100, totals.Subtotal);
        // 8% tax
        Assert.Equal(8, totals.TaxAmount);
        // Subtotal >= $50 threshold -> free standard shipping
        Assert.Equal(0, totals.ShippingCost);
        Assert.True(totals.IsFreeShipping);
        Assert.Equal(108, totals.GrandTotal);
    }

    [Fact]
    public async Task Totals_StandardUnderThreshold_ChargesFiveNinetyNine()
    {
        using var context = TestDb.CreateContext();
        var service = TestDb.CreateService(context);
        var product = TestDb.SeedProduct(context, 9002, price: 30, stock: 10);

        var totals = await service.CalculateOrderTotalsAsync(
            new List<CartItem> { TestDb.CartItemFor(product, 1) }, "Standard");

        Assert.Equal(30, totals.Subtotal);
        Assert.Equal(2.40m, totals.TaxAmount);
        Assert.Equal(5.99m, totals.ShippingCost);
        Assert.False(totals.IsFreeShipping);
        Assert.Equal(38.39m, totals.GrandTotal);
    }

    [Fact]
    public async Task Totals_Express_IsAlwaysFifteenEvenOverThreshold()
    {
        using var context = TestDb.CreateContext();
        var service = TestDb.CreateService(context);
        var product = TestDb.SeedProduct(context, 9003, price: 100, stock: 10);

        var totals = await service.CalculateOrderTotalsAsync(
            new List<CartItem> { TestDb.CartItemFor(product, 3) }, "Express");

        Assert.Equal(300, totals.Subtotal);
        Assert.Equal(15, totals.ShippingCost);
        Assert.False(totals.IsFreeShipping);
    }

    [Fact]
    public async Task Totals_FreeShipDiscount_NullifiesShippingRegardlessOfThreshold()
    {
        using var context = TestDb.CreateContext();
        var service = TestDb.CreateService(context);
        var product = TestDb.SeedProduct(context, 9004, price: 30, stock: 10);
        var cart = new List<CartItem> { TestDb.CartItemFor(product, 1) };

        var totals = await service.CalculateOrderTotalsAsync(cart, "Standard", "FREESHIP");

        Assert.Equal(0, totals.ShippingCost);
        Assert.Equal(30 + 2.40m, totals.GrandTotal);
    }

    [Fact]
    public async Task Totals_PercentageDiscount_ReducesGrandTotalButCollectsTax()
    {
        using var context = TestDb.CreateContext();
        var service = TestDb.CreateService(context);
        var product = TestDb.SeedProduct(context, 9005, price: 100, stock: 10);

        var totals = await service.CalculateOrderTotalsAsync(
            new List<CartItem> { TestDb.CartItemFor(product, 2) }, "Standard", "WELCOME10");

        // Subtotal 200, tax 16, free shipping (>=50), 10% = 20
        Assert.Equal(20, totals.DiscountAmount);
        Assert.Equal(200 + 16 + 0 - 20, totals.GrandTotal);
    }

    [Fact]
    public async Task Totals_EmptyCart_ReturnsZeros()
    {
        using var context = TestDb.CreateContext();
        var service = TestDb.CreateService(context);

        var totals = await service.CalculateOrderTotalsAsync(new List<CartItem>(), "Standard");

        Assert.Equal(0, totals.Subtotal);
        Assert.Equal(0, totals.GrandTotal);
    }
}