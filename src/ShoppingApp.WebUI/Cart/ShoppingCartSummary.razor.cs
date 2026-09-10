using Microsoft.AspNetCore.Components;
using MudBlazor;
using ShoppingApp.Abstractions;

namespace ShoppingApp.WebUI.Cart;

public partial class ShoppingCartSummary
{
    private string TotalCost => Items?.Sum(x => x.TotalPrice).ToString("C2") ?? "$0.00";

    [Parameter, EditorRequired]
    public HashSet<CartItem>? Items { get; set; }

    [Parameter, EditorRequired]
    public EventCallback OnCheckoutRequested { get; set; }

    [Parameter]
    public bool IsProcessing { get; set; }

    [Parameter]
    public string? CheckoutFeedbackMessage { get; set; }

    [Parameter]
    public Severity CheckoutFeedbackSeverity { get; set; } = Severity.Info;

    private Task CheckoutAsync() =>
        OnCheckoutRequested.HasDelegate
            ? OnCheckoutRequested.InvokeAsync()
            : Task.CompletedTask;
}