using Orleans.TestingHost;
using ShoppingApp.Abstractions;
using ShoppingApp.Tests.Initialization;
using Xunit;

namespace ShoppingApp.Tests;

[Collection(ClusterCollection.ClusterFixtureName)]
public class CheckoutTests(ClusterFixture clusterFixture)
{
    private TestCluster Cluster { get; } = clusterFixture.Cluster;

    [Fact]
    public async Task Checkout_OnEmptyCart_ReturnsEmptyCartFailure()
    {
        var cart = Cluster.GrainFactory.GetGrain<IShoppingCartGrain>(nameof(Checkout_OnEmptyCart_ReturnsEmptyCartFailure));

        var result = await cart.CheckoutAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal(CheckoutFailureReason.EmptyCart, result.FailureReason);
    }

    [Fact]
    public async Task AddToCart_DoesNotReserveStock_BeforeCheckout()
    {
        var product = new ProductDetails
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = "Test product",
            Quantity = 5,
            UnitPrice = 10m,
            Description = "desc",
            DetailsUrl = "https://example.invalid/details",
            ImageUrl = "https://example.invalid/image"
        };

        var productGrain = Cluster.GrainFactory.GetGrain<IProductGrain>(product.Id);
        var cart = Cluster.GrainFactory.GetGrain<IShoppingCartGrain>(nameof(AddToCart_DoesNotReserveStock_BeforeCheckout));

        await productGrain.CreateOrUpdateProductAsync(product);
        var added = await cart.AddOrUpdateItemAsync(3, product);
        var availability = await productGrain.GetProductAvailabilityAsync();

        Assert.True(added);
        Assert.Equal(5, availability);
    }

    [Fact]
    public async Task Checkout_Fails_WhenStockBecomesUnavailable_AfterAddingToCart()
    {
        var product = new ProductDetails
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = "Another product",
            Quantity = 1,
            UnitPrice = 20m,
            Description = "desc",
            DetailsUrl = "https://example.invalid/details",
            ImageUrl = "https://example.invalid/image"
        };

        var productGrain = Cluster.GrainFactory.GetGrain<IProductGrain>(product.Id);
        var cart = Cluster.GrainFactory.GetGrain<IShoppingCartGrain>(nameof(Checkout_Fails_WhenStockBecomesUnavailable_AfterAddingToCart));

        await productGrain.CreateOrUpdateProductAsync(product);
        Assert.True(await cart.AddOrUpdateItemAsync(1, product));

        var reservation = await productGrain.TryTakeProductAsync(1);
        Assert.True(reservation.IsAvailable);

        var result = await cart.CheckoutAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal(CheckoutFailureReason.OutOfStock, result.FailureReason);
    }
}
