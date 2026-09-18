using JetBrains.Annotations;
using Orleans.Runtime;
using ShoppingApp.Abstractions;

namespace ShoppingApp.Grains;

[UsedImplicitly]
public sealed class OrderGrain(
    [PersistentState(stateName: "Order", storageName: PersistentStorageConfig.AzureSqlName)]
    IPersistentState<OrderDetails> state)
    : Grain, IOrderGrain
{
    public Task CreateAsync(OrderDetails order)
    {
        state.State = order;
        return state.WriteStateAsync();
    }

    public Task<OrderDetails?> GetAsync() =>
        state.RecordExists
            ? Task.FromResult<OrderDetails?>(state.State)
            : Task.FromResult<OrderDetails?>(null);

    public async Task UpdateStatusAsync(OrderStatus status, string? failureReason, string? paymentTransactionId)
    {
        if (!state.RecordExists)
        {
            return;
        }

        state.State = state.State with
        {
            Status = status,
            FailureReason = failureReason,
            PaymentTransactionId = paymentTransactionId
        };

        await state.WriteStateAsync();
    }
}
