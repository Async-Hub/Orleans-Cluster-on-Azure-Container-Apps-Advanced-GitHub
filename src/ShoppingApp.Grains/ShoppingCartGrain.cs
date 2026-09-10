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

      var reservedProducts = new List<(IProductGrain Grain, int Quantity, string ProductName)>();
      foreach (var item in cartItems)
      {
        var productGrain = GrainFactory.GetGrain<IProductGrain>(item.Product.Id);
        var (isTaken, _) = await productGrain.TryTakeProductAsync(item.Quantity);
        if (!isTaken)
        {
          foreach (var (grain, quantity, _) in reservedProducts)
          {
            await grain.ReturnProductAsync(quantity);
          }

          var unavailableItems = reservedProducts.Select(x => x.ProductName).Append(item.Product.Name).Distinct();
          var message = $"Some items are out of stock: {string.Join(", ", unavailableItems)}.";
          return new CheckoutResult(false, CheckoutFailureReason.OutOfStock, message, null);
        }

        reservedProducts.Add((productGrain, item.Quantity, item.Product.Name));
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
        foreach (var (grain, quantity, _) in reservedProducts)
        {
          await grain.ReturnProductAsync(quantity);
        }

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
