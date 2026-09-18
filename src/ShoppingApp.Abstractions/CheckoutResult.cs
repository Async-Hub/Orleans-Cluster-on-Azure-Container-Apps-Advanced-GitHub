namespace ShoppingApp.Abstractions;

[GenerateSerializer, Immutable]
public sealed record CheckoutResult(
    [property: Id(0)] bool IsSuccess,
    [property: Id(1)] CheckoutFailureReason FailureReason,
    [property: Id(2)] string Message,
    [property: Id(3)] string? OrderId);
