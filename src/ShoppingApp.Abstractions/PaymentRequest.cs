namespace ShoppingApp.Abstractions;

[GenerateSerializer, Immutable]
public sealed record PaymentRequest(
    [property: Id(0)] string OrderId,
    [property: Id(1)] string UserId,
    [property: Id(2)] decimal Amount,
    [property: Id(3)] string CurrencyCode);
