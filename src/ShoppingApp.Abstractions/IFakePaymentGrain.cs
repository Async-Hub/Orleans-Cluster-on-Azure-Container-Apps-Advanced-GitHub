namespace ShoppingApp.Abstractions;

public interface IFakePaymentGrain : IGrainWithStringKey
{
    Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request);
}
