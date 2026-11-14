namespace MonoBank.Acquiring;

public sealed partial class MonoAcquiringClient
{
    public async Task<IReadOnlyList<QrCashier>> GetQrListAsync(CancellationToken cancellationToken = default)
    {
        var page = await SendAsync<QrListResponse>(HttpMethod.Get, "api/merchant/qr/list", null, cancellationToken).ConfigureAwait(false);
        return page.List ?? [];
    }

    public Task<QrDetails> GetQrDetailsAsync(string qrId, CancellationToken cancellationToken = default)
    {
        RequireId(qrId, nameof(qrId));
        return SendAsync<QrDetails>(HttpMethod.Get, "api/merchant/qr/details", null, cancellationToken, [Q("qrId", qrId)]);
    }

    public Task ResetQrAmountAsync(string qrId, CancellationToken cancellationToken = default)
    {
        RequireId(qrId, nameof(qrId));
        return SendAsync(HttpMethod.Post, "api/merchant/qr/reset-amount", new QrIdBody(qrId), cancellationToken);
    }

    public Task<MerchantDetails> GetMerchantDetailsAsync(CancellationToken cancellationToken = default) =>
        SendAsync<MerchantDetails>(HttpMethod.Get, "api/merchant/details", null, cancellationToken);

    public async Task<IReadOnlyList<StatementItem>> GetStatementAsync(
        long from,
        long? to = null,
        string? code = null,
        CancellationToken cancellationToken = default)
    {
        var page = await SendAsync<StatementResponse>(
            HttpMethod.Get,
            "api/merchant/statement",
            null,
            cancellationToken,
            [Q("from", from), Q("to", to), Q("code", code)]).ConfigureAwait(false);
        return page.List ?? [];
    }

    public async Task<IReadOnlyList<Employee>> GetEmployeesAsync(CancellationToken cancellationToken = default)
    {
        var page = await SendAsync<EmployeeListResponse>(HttpMethod.Get, "api/merchant/employee/list", null, cancellationToken).ConfigureAwait(false);
        return page.List ?? [];
    }

    public async Task<IReadOnlyList<Submerchant>> GetSubmerchantsAsync(CancellationToken cancellationToken = default)
    {
        var page = await SendAsync<SubmerchantListResponse>(HttpMethod.Get, "api/merchant/submerchant/list", null, cancellationToken).ConfigureAwait(false);
        return page.List ?? [];
    }

    public Task<PubKeyResponse> GetPubKeyAsync(CancellationToken cancellationToken = default) =>
        SendAsync<PubKeyResponse>(HttpMethod.Get, "api/merchant/pubkey", null, cancellationToken);

    private sealed class QrIdBody(string qrId)
    {
        public string QrId { get; } = qrId;
    }

    private sealed class QrListResponse
    {
        public IReadOnlyList<QrCashier>? List { get; set; }
    }

    private sealed class StatementResponse
    {
        public IReadOnlyList<StatementItem>? List { get; set; }
    }

    private sealed class EmployeeListResponse
    {
        public IReadOnlyList<Employee>? List { get; set; }
    }

    private sealed class SubmerchantListResponse
    {
        public IReadOnlyList<Submerchant>? List { get; set; }
    }
}

public sealed class QrCashier
{
    public string? QrId { get; init; }

    public string? ShortQrId { get; init; }

    public string? AmountType { get; init; }

    public string? PageUrl { get; init; }
}

public sealed class QrDetails
{
    public string? ShortQrId { get; init; }

    public long? Amount { get; init; }

    public int? Ccy { get; init; }

    public string? InvoiceId { get; init; }
}

public sealed class MerchantDetails
{
    public string? MerchantId { get; init; }

    public string? MerchantName { get; init; }

    public string? Edrpou { get; init; }
}

public sealed class StatementItem
{
    public string? InvoiceId { get; init; }

    public string? Status { get; init; }

    public string? MaskedPan { get; init; }

    public DateTimeOffset? Date { get; init; }

    public string? PaymentScheme { get; init; }

    public long Amount { get; init; }

    public long? ProfitAmount { get; init; }

    public int Ccy { get; init; }

    public string? ApprovalCode { get; init; }

    public string? Rrn { get; init; }

    public string? Reference { get; init; }

    public string? ShortQrId { get; init; }

    public string? Destination { get; init; }

    public IReadOnlyList<StatementRefund>? CancelList { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}

public sealed class StatementRefund
{
    public long Amount { get; init; }

    public int Ccy { get; init; }

    public DateTimeOffset? Date { get; init; }

    public string? ApprovalCode { get; init; }

    public string? Rrn { get; init; }

    public string? MaskedPan { get; init; }
}

public sealed class Employee
{
    public string? Id { get; init; }

    public string? Name { get; init; }

    public string? ExtRef { get; init; }
}

public sealed class Submerchant
{
    public string? Code { get; init; }

    public string? Edrpou { get; init; }

    public string? Iban { get; init; }

    public string? Owner { get; init; }
}

public sealed class PubKeyResponse
{
    public string? Key { get; init; }
}
