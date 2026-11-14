using System.Text.Json.Serialization;

namespace MonoBank.Acquiring;

public sealed partial class MonoAcquiringClient
{
    public async Task<IReadOnlyList<Terminal>> GetTerminalsAsync(CancellationToken cancellationToken = default)
    {
        var page = await SendAsync<TerminalListResponse>(HttpMethod.Get, "api/merchant/t2p/terminal/list", null, cancellationToken).ConfigureAwait(false);
        return page.List ?? [];
    }

    public Task<T2pPaymentStatus> GetT2pPaymentStatusAsync(string externalPaymentId, CancellationToken cancellationToken = default)
    {
        RequireId(externalPaymentId, nameof(externalPaymentId));
        return SendAsync<T2pPaymentStatus>(
            HttpMethod.Get,
            "api/merchant/t2p/terminal/payment/external/status",
            null,
            cancellationToken,
            [Q("externalPaymentId", externalPaymentId)]);
    }

    private sealed class TerminalListResponse
    {
        public IReadOnlyList<Terminal>? List { get; set; }
    }
}

public sealed class Terminal
{
    public string? Code { get; init; }

    public string? Name { get; init; }

    [JsonPropertyName("terminal")]
    public string? TerminalId { get; init; }
}

public sealed class T2pPaymentStatus
{
    public string? Bank { get; init; }

    public string? Terminal { get; init; }

    public string? PaymentType { get; init; }

    public long Amount { get; init; }

    [JsonConverter(typeof(JsonStringOrNumberConverter))]
    public string? Ccy { get; init; }

    public string? Status { get; init; }

    public string? InternalPaymentId { get; init; }

    public string? ExternalPaymentId { get; init; }

    public string? TransactionId { get; init; }

    public string? MaskedPan { get; init; }

    public string? CardMask { get; init; }

    public string? ApprovalCode { get; init; }

    public string? Rrn { get; init; }

    [JsonPropertyName("dataTime")]
    public string? DataTime { get; init; }

    public string? ResponseCode { get; init; }

    public string? CountryCard { get; init; }

    public string? ErrorMessage { get; init; }
}
