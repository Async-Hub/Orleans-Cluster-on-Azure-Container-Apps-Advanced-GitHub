namespace ShoppingApp.Abstractions;

[GenerateSerializer, Immutable]
public sealed record OrderDetails(
    [property: Id(0)] string OrderId,
    [property: Id(1)] string UserId,
    [property: Id(2)] DateTimeOffset CreatedAt,
    [property: Id(3)] decimal TotalAmount,
    [property: Id(4)] OrderStatus Status,
    [property: Id(5)] string? FailureReason,
    [property: Id(6)] string? PaymentTransactionId,
    [property: Id(7)] HashSet<OrderLineItem> Items);
