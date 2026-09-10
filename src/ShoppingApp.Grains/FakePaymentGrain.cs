using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using ShoppingApp.Abstractions;

namespace ShoppingApp.Grains;

[UsedImplicitly]
public sealed class FakePaymentGrain(ILogger<FakePaymentGrain> logger) : Grain, IFakePaymentGrain
{
    private const decimal FailureRate = 0.2m;
    private const string ForcedFailurePrefixEnvironmentVariable = "SHOPPINGAPP_FAKEPAYMENT_FORCE_FAILURE_FOR_USERID_PREFIX";
    private const string PaymentDelayMsEnvironmentVariable = "SHOPPINGAPP_FAKEPAYMENT_DELAY_MS";

    public async Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request)
    {
        logger.LogInformation("Processing fake payment for OrderId {OrderId}, Amount {Amount}", request.OrderId, request.Amount);

        if (int.TryParse(Environment.GetEnvironmentVariable(PaymentDelayMsEnvironmentVariable), out var delayMs) && delayMs > 0)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(delayMs));
        }
        else
        {
            await Task.Delay(TimeSpan.FromMilliseconds(2000));
        }

        var forcedFailurePrefix = Environment.GetEnvironmentVariable(ForcedFailurePrefixEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(forcedFailurePrefix) && request.UserId.StartsWith(forcedFailurePrefix, StringComparison.Ordinal))
        {
            const string failureReason = "Payment authorization was declined by the mock gateway.";
            logger.LogWarning("Fake payment forced to fail for OrderId {OrderId}. Reason: {Reason}", request.OrderId, failureReason);
            return new PaymentResult(false, null, failureReason);
        }

        var roll = Random.Shared.NextDouble();
        if ((decimal)roll < FailureRate)
        {
            const string failureReason = "Payment authorization was declined by the mock gateway.";
            logger.LogWarning("Fake payment failed for OrderId {OrderId}. Reason: {Reason}", request.OrderId, failureReason);
            return new PaymentResult(false, null, failureReason);
        }

        var transactionId = $"MOCK-{Guid.NewGuid():N}";
        logger.LogInformation("Fake payment succeeded for OrderId {OrderId}, TransactionId {TransactionId}", request.OrderId, transactionId);

        return new PaymentResult(true, transactionId, null);
    }
}
