namespace ShoppingApp.Abstractions;

public interface IOrderGrain : IGrainWithStringKey
{
    Task CreateAsync(OrderDetails order);

    Task<OrderDetails?> GetAsync();

    Task UpdateStatusAsync(OrderStatus status, string? failureReason, string? paymentTransactionId);
}
