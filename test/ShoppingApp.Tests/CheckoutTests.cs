using Orleans.TestingHost;
using ShoppingApp.Abstractions;
using ShoppingApp.Tests.Initialization;
using Xunit;

namespace ShoppingApp.Tests;

[Collection(ClusterCollection.ClusterFixtureName)]
public class CheckoutTests(ClusterFixture clusterFixture)
{
    private const string ForcedFailurePrefixEnvironmentVariable = "SHOPPINGAPP_FAKEPAYMENT_FORCE_FAILURE_FOR_USERID_PREFIX";
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
        var product = CreateProduct(quantity: 5, unitPrice: 10m);
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
        var product = CreateProduct(quantity: 1, unitPrice: 20m);
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

    [Fact]
    public async Task Checkout_ReturnsPaymentFailure_AndRestoresStock()
    {
        var userId = $"force-payment-failure-{Guid.NewGuid():N}";
        var previousPrefix = Environment.GetEnvironmentVariable(ForcedFailurePrefixEnvironmentVariable);
        Environment.SetEnvironmentVariable(ForcedFailurePrefixEnvironmentVariable, "force-payment-failure-");

        try
        {
            var product = CreateProduct(quantity: 2, unitPrice: 15m);
            var productGrain = Cluster.GrainFactory.GetGrain<IProductGrain>(product.Id);
            var cart = Cluster.GrainFactory.GetGrain<IShoppingCartGrain>(userId);

            await productGrain.CreateOrUpdateProductAsync(product);
            Assert.True(await cart.AddOrUpdateItemAsync(2, product));

            var result = await cart.CheckoutAsync();
            var availability = await productGrain.GetProductAvailabilityAsync();

            Assert.False(result.IsSuccess);
            Assert.Equal(CheckoutFailureReason.PaymentFailed, result.FailureReason);
            Assert.NotNull(result.OrderId);
            Assert.Equal(2, availability);

            var order = await Cluster.GrainFactory.GetGrain<IOrderGrain>(result.OrderId!).GetAsync();
            Assert.NotNull(order);
            Assert.Equal(OrderStatus.Failed, order!.Status);
            Assert.False(string.IsNullOrWhiteSpace(order.FailureReason));
        }
        finally
        {
            Environment.SetEnvironmentVariable(ForcedFailurePrefixEnvironmentVariable, previousPrefix);
        }
    }

    private static ProductDetails CreateProduct(int quantity, decimal unitPrice) =>
        new()
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = "Test product",
            Quantity = quantity,
            UnitPrice = unitPrice,
            Description = "desc",
            DetailsUrl = "https://example.invalid/details",
            ImageUrl = "https://example.invalid/image"
        };
}
