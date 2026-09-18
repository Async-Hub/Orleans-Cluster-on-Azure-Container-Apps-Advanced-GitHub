namespace ShoppingApp.Abstractions;

public enum CheckoutFailureReason
{
    None = 0,
    EmptyCart = 1,
    OutOfStock = 2,
    PaymentFailed = 3,
    Unknown = 4
}
