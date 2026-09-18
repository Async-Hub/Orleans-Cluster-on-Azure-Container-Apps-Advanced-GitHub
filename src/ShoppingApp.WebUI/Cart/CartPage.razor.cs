using Microsoft.AspNetCore.Components;
using MudBlazor;
using ShoppingApp.Abstractions;
using ShoppingApp.WebUI.Services;

namespace ShoppingApp.WebUI.Cart;

public sealed partial class CartPage
{
  private HashSet<CartItem>? _cartItems;
  private bool _isProcessingCheckout;
  private string? _checkoutFeedbackMessage = string.Empty;
  private Severity _checkoutFeedbackSeverity = Severity.Info;

  [Inject]
  public ShoppingCartService ShoppingCart { get; set; } = null!;

  [Inject]
  public ComponentStateChangedObserver Observer { get; set; } = null!;

  [Inject]
  public ToastService ToastService { get; set; } = null!;

  protected override Task OnInitializedAsync() => GetCartItemsAsync();

  private Task GetCartItemsAsync() =>
      InvokeAsync(async () =>
      {
        _cartItems = await ShoppingCart.GetAllItemsAsync();
        StateHasChanged();
      });

  private async Task OnItemRemovedAsync(ProductDetails product)
  {
    await ShoppingCart.RemoveItemAsync(product);
    await Observer.NotifyStateChangedAsync();

    _ = _cartItems?.RemoveWhere(item => item.Product == product);
  }

  private async Task OnItemUpdatedAsync((int Quantity, ProductDetails Product) tuple)
  {
    var isUpdated = await ShoppingCart.AddOrUpdateItemAsync(tuple.Quantity, tuple.Product);
    if (!isUpdated)
    {
      _checkoutFeedbackMessage = $"Could not update '{tuple.Product.Name}' because requested quantity is unavailable.";
      _checkoutFeedbackSeverity = Severity.Warning;
      await ToastService.ShowToastAsync("Quantity unavailable", _checkoutFeedbackMessage);
    }

    await GetCartItemsAsync();
  }

  private async Task EmptyCartAsync()
  {
    await ShoppingCart.EmptyCartAsync();
    await Observer.NotifyStateChangedAsync();

    _cartItems?.Clear();
  }

  private async Task CheckoutAsync()
  {
    if (_isProcessingCheckout)
    {
      return;
    }

    _checkoutFeedbackMessage = null;
    _isProcessingCheckout = true;

    try
    {
      var result = await ShoppingCart.CheckoutAsync();
      _checkoutFeedbackMessage = result.Message;

      if (result.IsSuccess)
      {
        _checkoutFeedbackSeverity = Severity.Success;
        await ToastService.ShowToastAsync("Payment succeeded", $"Order {result.OrderId} has been paid.");
        await GetCartItemsAsync();
        await Observer.NotifyStateChangedAsync();
        return;
      }

      _checkoutFeedbackSeverity = result.FailureReason switch
      {
        CheckoutFailureReason.OutOfStock => Severity.Warning,
        CheckoutFailureReason.PaymentFailed => Severity.Error,
        CheckoutFailureReason.EmptyCart => Severity.Info,
        _ => Severity.Error
      };

      await ToastService.ShowToastAsync("Checkout failed", result.Message);
    }
    finally
    {
      _isProcessingCheckout = false;
    }
  }
}