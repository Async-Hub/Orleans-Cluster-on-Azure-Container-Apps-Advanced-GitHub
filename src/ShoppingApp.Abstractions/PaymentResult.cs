namespace ShoppingApp.Abstractions;

[GenerateSerializer, Immutable]
public sealed record PaymentResult(
    [property: Id(0)] bool IsSuccess,
    [property: Id(1)] string? TransactionId,
    [property: Id(2)] string? FailureReason);
