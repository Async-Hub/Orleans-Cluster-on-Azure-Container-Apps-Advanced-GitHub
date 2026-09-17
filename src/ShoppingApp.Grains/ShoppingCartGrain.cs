using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Orleans.Concurrency;
using ShoppingApp.Abstractions;

namespace ShoppingApp.Grains;

[Reentrant]
[UsedImplicitly]
public sealed class ShoppingCartGrain(
    [PersistentState(stateName: "ShoppingCart", storageName: PersistentStorageConfig.AzureStorageName)]
    IPersistentState<Dictionary<string, CartItem>> cart,
    ILogger<ShoppingCartGrain> logger)
    : Grain, IShoppingCartGrain
{
  async Task<bool> IShoppingCartGrain.AddOrUpdateItemAsync(int quantity, ProductDetails product)
  {
    if (quantity <= 0)
    {
      return false;
    }

    var products = GrainFactory.GetGrain<IProductGrain>(product.Id);
    var available = await products.GetProductAvailabilityAsync();
    if (available < quantity)
    {
      return false;
    }

    var productDetails = await products.GetProductDetailsAsync();
    var item = ToCartItem(quantity, productDetails with { Quantity = available });
    cart.State[productDetails.Id] = item;

    await cart.WriteStateAsync();
    return true;
  }

  async Task<CheckoutResult> IShoppingCartGrain.CheckoutAsync()
  {
    try
    {
      if (cart.State.Count == 0)
      {
        return new CheckoutResult(false, CheckoutFailureReason.EmptyCart, 
          "Your cart is empty.", null);
      }

      var cartItems = cart.State.Values.ToList();
      if (cartItems.Any(i => i.Quantity <= 0))
      {
        return new CheckoutResult(false, CheckoutFailureReason.Unknown, 
          "Cart contains invalid quantities.", null);
      }

      var outOfStockItems = new List<string>();
      foreach (var item in cartItems)
      {
        var productGrain = GrainFactory.GetGrain<IProductGrain>(item.Product.Id);
        var available = await productGrain.GetProductAvailabilityAsync();
        if (available < item.Quantity)
        {
          outOfStockItems.Add(item.Product.Name);
        }
      }

      if (outOfStockItems.Count > 0)
      {
        var message = $"Some items are out of stock: {string.Join(", ", outOfStockItems)}.";
        return new CheckoutResult(false, CheckoutFailureReason.OutOfStock, message, null);
      }

      var orderId = $"ORD-{Guid.NewGuid():N}";
      var totalAmount = cartItems.Sum(i => i.TotalPrice);
      var order = new OrderDetails(
          orderId,
          this.GetPrimaryKeyString(),
          DateTimeOffset.UtcNow,
          totalAmount,
          OrderStatus.Pending,
          null,
          null,
          cartItems
              .Select(ToOrderLineItem)
              .ToHashSet());

      var orderGrain = GrainFactory.GetGrain<IOrderGrain>(orderId);
      await orderGrain.CreateAsync(order);

      var paymentGrain = GrainFactory.GetGrain<IFakePaymentGrain>("MockPaymentProvider");
      var paymentResult = await paymentGrain.ProcessPaymentAsync(
          new PaymentRequest(orderId, this.GetPrimaryKeyString(), totalAmount, "USD"));

      if (!paymentResult.IsSuccess)
      {
        await orderGrain.UpdateStatusAsync(
            OrderStatus.Failed,
            paymentResult.FailureReason,
            paymentResult.TransactionId);

        return new CheckoutResult(
            false,
            CheckoutFailureReason.PaymentFailed,
            paymentResult.FailureReason ?? "Payment failed.",
            orderId);
      }

      var decrementedProducts = new List<(IProductGrain Grain, int Quantity)>();
      try
      {
        foreach (var item in cartItems)
        {
          var productGrain = GrainFactory.GetGrain<IProductGrain>(item.Product.Id);
          var (isTaken, _) = await productGrain.TryTakeProductAsync(item.Quantity);
          if (!isTaken)
          {
            throw new InvalidOperationException($"Product '{item.Product.Name}' became unavailable.");
          }

          decrementedProducts.Add((productGrain, item.Quantity));
        }
      }
      catch (Exception ex)
      {
        logger.LogWarning(ex, "Checkout stock update failed for user {UserId}. Rolling back stock updates.", this.GetPrimaryKeyString());

        foreach (var (grain, quantity) in decrementedProducts)
        {
          await grain.ReturnProductAsync(quantity);
        }

        const string message = "Checkout failed because stock changed during payment processing. Please try again.";
        await orderGrain.UpdateStatusAsync(OrderStatus.Failed, message, paymentResult.TransactionId);

        return new CheckoutResult(false, CheckoutFailureReason.OutOfStock, message, orderId);
      }

      cart.State.Clear();
      await cart.ClearStateAsync();

      await orderGrain.UpdateStatusAsync(OrderStatus.Paid, null, paymentResult.TransactionId);

      logger.LogInformation("Checkout completed successfully for user {UserId}, order {OrderId}", this.GetPrimaryKeyString(), orderId);
      return new CheckoutResult(true, CheckoutFailureReason.None, "Payment successful.", orderId);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Unexpected checkout failure for user {UserId}", this.GetPrimaryKeyString());
      return new CheckoutResult(false, CheckoutFailureReason.Unknown, "Unexpected checkout error.", null);
    }
  }

  Task IShoppingCartGrain.EmptyCartAsync()
  {
    cart.State.Clear();
    return cart.ClearStateAsync();
  }

  Task<HashSet<CartItem>> IShoppingCartGrain.GetAllItemsAsync() =>
      Task.FromResult(cart.State.Values.ToHashSet());

  Task<int> IShoppingCartGrain.GetTotalItemsInCartAsync() =>
      Task.FromResult(cart.State.Count);

  async Task IShoppingCartGrain.RemoveItemAsync(ProductDetails product)
  {
    if (cart.State.Remove(product.Id))
    {
      await cart.WriteStateAsync();
    }
  }

  private CartItem ToCartItem(int quantity, ProductDetails product) =>
      new(this.GetPrimaryKeyString(), quantity, product);

  private static OrderLineItem ToOrderLineItem(CartItem item) =>
      new(
          item.Product.Id,
          item.Product.Name,
          item.Quantity,
          item.Product.UnitPrice,
          item.TotalPrice);
}