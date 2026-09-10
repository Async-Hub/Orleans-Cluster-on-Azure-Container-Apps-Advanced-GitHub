namespace ShoppingApp.Abstractions;

[GenerateSerializer, Immutable]
public sealed record OrderLineItem(
    [property: Id(0)] string ProductId,
    [property: Id(1)] string ProductName,
    [property: Id(2)] int Quantity,
    [property: Id(3)] decimal UnitPrice,
    [property: Id(4)] decimal TotalPrice);
