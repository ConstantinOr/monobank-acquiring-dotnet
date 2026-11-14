using System.Text.Json;
using System.Text.Json.Serialization;

namespace MonoBank.Acquiring;

public sealed partial class MonoAcquiringClient
{
    public Task<PosRefund> CancelPosTransactionAsync(
        string rrn,
        long amount,
        string acceptLanguage = "uk",
        CancellationToken cancellationToken = default)
    {
        RequireId(rrn, nameof(rrn));
        RequireId(acceptLanguage, nameof(acceptLanguage));
        return SendAllowEmptyAsync<PosRefund>(
            HttpMethod.Post,
            "api/merchant/pos-transaction-cancel",
            new PosCancelBody(rrn, amount),
            cancellationToken,
            acceptLanguage);
    }

    public Task<PosRefund> GetPosCancelStatusAsync(string cancelRef, CancellationToken cancellationToken = default)
    {
        RequireId(cancelRef, nameof(cancelRef));
        return SendAsync<PosRefund>(
            HttpMethod.Get,
            "api/merchant/pos-transaction-cancel/status",
            null,
            cancellationToken,
            [Q("cancelRef", cancelRef)]);
    }

    private sealed class PosCancelBody(string rrn, long amount)
    {
        public string Rrn { get; } = rrn;

        public long Amount { get; } = amount;
    }
}

public sealed class PosRefund
{
    public string? CancelRef { get; init; }

    public string? Status { get; init; }

    public string? Rrn { get; init; }

    public long? Amount { get; init; }

    public int? Ccy { get; init; }

    public DateTimeOffset? CreatedDate { get; init; }

    public DateTimeOffset? ModifiedDate { get; init; }

    public string? FailureReason { get; init; }

    // ponytail: 200 sample is rendered client-side and has no published field list. Unknown fields stay here.
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? Extra { get; init; }
}
