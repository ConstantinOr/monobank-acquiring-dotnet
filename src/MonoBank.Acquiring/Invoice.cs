using System.Text.Json;

namespace MonoBank.Acquiring;

public sealed partial class MonoAcquiringClient
{
    public async Task<CreateInvoiceResponse> CreateInvoiceAsync(CreateInvoiceRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return await SendAsync<CreateInvoiceResponse>(HttpMethod.Post, "api/merchant/invoice/create", request, cancellationToken).ConfigureAwait(false);
    }

    public async Task<InvoiceStatus> GetInvoiceStatusAsync(string invoiceId, CancellationToken cancellationToken = default)
    {
        RequireId(invoiceId, nameof(invoiceId));
        return await SendAsync<InvoiceStatus>(
            HttpMethod.Get,
            "api/merchant/invoice/status",
            null,
            cancellationToken,
            [Q("invoiceId", invoiceId)]).ConfigureAwait(false);
    }

    public async Task<CancelInvoiceResponse> CancelInvoiceAsync(CancelInvoiceRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        RequireId(request.InvoiceId, nameof(request.InvoiceId));
        return await SendAsync<CancelInvoiceResponse>(HttpMethod.Post, "api/merchant/invoice/cancel", request, cancellationToken).ConfigureAwait(false);
    }

    public Task RemoveInvoiceAsync(string invoiceId, CancellationToken cancellationToken = default)
    {
        RequireId(invoiceId, nameof(invoiceId));
        return SendAsync(HttpMethod.Post, "api/merchant/invoice/remove", new InvoiceIdBody(invoiceId), cancellationToken);
    }

    public async Task<FinalizeInvoiceResponse> FinalizeInvoiceAsync(FinalizeInvoiceRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        RequireId(request.InvoiceId, nameof(request.InvoiceId));
        return await SendAsync<FinalizeInvoiceResponse>(HttpMethod.Post, "api/merchant/invoice/finalize", request, cancellationToken).ConfigureAwait(false);
    }

    public Task<ReceiptResponse> GetReceiptAsync(string invoiceId, string? email = null, CancellationToken cancellationToken = default)
    {
        RequireId(invoiceId, nameof(invoiceId));
        return SendAsync<ReceiptResponse>(
            HttpMethod.Get,
            "api/merchant/invoice/receipt",
            null,
            cancellationToken,
            [Q("invoiceId", invoiceId), Q("email", email)]);
    }

    public async Task<IReadOnlyList<FiscalCheck>> GetFiscalChecksAsync(string invoiceId, CancellationToken cancellationToken = default)
    {
        RequireId(invoiceId, nameof(invoiceId));
        var page = await SendAsync<FiscalChecksResponse>(
            HttpMethod.Get,
            "api/merchant/invoice/fiscal-checks",
            null,
            cancellationToken,
            [Q("invoiceId", invoiceId)]).ConfigureAwait(false);
        return page.Checks ?? [];
    }

    /// <summary>POST /api/merchant/invoice/payment-direct. Body contains PAN and CVV. The merchant needs a PCI DSS certificate.</summary>
    public async Task<PaymentResult> PaymentDirectAsync(PaymentDirectRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return await SendAsync<PaymentResult>(HttpMethod.Post, "api/merchant/invoice/payment-direct", request, cancellationToken).ConfigureAwait(false);
    }

    public async Task<InvoiceStatus> SyncPaymentAsync(SyncPaymentRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return await SendAsync<InvoiceStatus>(HttpMethod.Post, "api/merchant/invoice/sync-payment", request, cancellationToken).ConfigureAwait(false);
    }

    private sealed class InvoiceIdBody(string invoiceId)
    {
        public string InvoiceId { get; } = invoiceId;
    }

    private sealed class FiscalChecksResponse
    {
        public IReadOnlyList<FiscalCheck>? Checks { get; set; }
    }
}

public sealed class CreateInvoiceRequest
{
    public required long Amount { get; init; }

    public int? Ccy { get; init; }

    public MerchantPaymInfo? MerchantPaymInfo { get; init; }

    public string? RedirectUrl { get; init; }

    public string? SuccessUrl { get; init; }

    public string? FailUrl { get; init; }

    public string? WebHookUrl { get; init; }

    public int? Validity { get; init; }

    public string? PaymentType { get; init; }

    public string? QrId { get; init; }

    public string? Code { get; init; }

    public SaveCardData? SaveCardData { get; init; }

    public string? TipsEmployeeId { get; init; }

    public decimal? AgentFeePercent { get; init; }

    public string? DisplayType { get; init; }

    public bool? WithAppUrl { get; init; }
}

public sealed class CreateInvoiceResponse
{
    public string? InvoiceId { get; init; }

    public string? PageUrl { get; init; }

    public string? AppUrl { get; init; }
}

public sealed class MerchantPaymInfo
{
    public string? Reference { get; init; }

    public string? Destination { get; init; }

    public string? Comment { get; init; }

    public IReadOnlyList<string>? CustomerEmails { get; init; }

    public IReadOnlyList<BasketAdjustment>? Discounts { get; init; }

    public IReadOnlyList<BasketItem>? BasketOrder { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}

public sealed class BasketItem
{
    public required string Name { get; init; }

    public required decimal Qty { get; init; }

    public required long Sum { get; init; }

    public long? Total { get; init; }

    public string? Icon { get; init; }

    public string? Unit { get; init; }

    public required string Code { get; init; }

    public string? Barcode { get; init; }

    public string? Header { get; init; }

    public string? Footer { get; init; }

    public IReadOnlyList<int>? Tax { get; init; }

    public string? Uktzed { get; init; }

    public IReadOnlyList<BasketAdjustment>? Discounts { get; init; }

    public string? SplitReceiverId { get; init; }
}

public sealed class BasketAdjustment
{
    public required string Type { get; init; }

    public required string Mode { get; init; }

    public required decimal Value { get; init; }

    public IReadOnlyList<int>? Tax { get; init; }
}

public sealed class SaveCardData
{
    public required bool SaveCard { get; init; }

    public string? WalletId { get; init; }
}

public sealed class InvoiceStatus
{
    public string? InvoiceId { get; init; }

    public string? Status { get; init; }

    public string? FailureReason { get; init; }

    public string? ErrCode { get; init; }

    public long Amount { get; init; }

    public int Ccy { get; init; }

    public long? FinalAmount { get; init; }

    public DateTimeOffset? CreatedDate { get; init; }

    public DateTimeOffset? ModifiedDate { get; init; }

    public string? Reference { get; init; }

    public string? Destination { get; init; }

    public IReadOnlyList<CancelOperation>? CancelList { get; init; }

    public PaymentInfo? PaymentInfo { get; init; }

    public TipsInfo? TipsInfo { get; init; }

    public WalletData? WalletData { get; init; }

    public static InvoiceStatus Parse(ReadOnlySpan<byte> utf8Json) =>
        JsonSerializer.Deserialize<InvoiceStatus>(utf8Json, AcquiringJson.Options)
        ?? throw new JsonException("Invoice payload is empty.");
}

public sealed class PaymentInfo
{
    public string? MaskedPan { get; init; }

    public string? ApprovalCode { get; init; }

    public string? Rrn { get; init; }

    public string? TranId { get; init; }

    public string? Terminal { get; init; }

    public string? Bank { get; init; }

    public string? PaymentSystem { get; init; }

    public string? PaymentMethod { get; init; }

    public string? Country { get; init; }

    public long? Fee { get; init; }

    public long? AgentFee { get; init; }
}

public sealed class CancelOperation
{
    public string? Status { get; init; }

    public long? Amount { get; init; }

    public int? Ccy { get; init; }

    public DateTimeOffset? CreatedDate { get; init; }

    public DateTimeOffset? ModifiedDate { get; init; }

    public string? ApprovalCode { get; init; }

    public string? Rrn { get; init; }

    public string? ExtRef { get; init; }
}

public sealed class TipsInfo
{
    public string? EmployeeId { get; init; }

    public long? Amount { get; init; }
}

public sealed class WalletData
{
    public string? CardToken { get; init; }

    public string? WalletId { get; init; }

    public string? Status { get; init; }
}

public sealed class CancelInvoiceRequest
{
    public required string InvoiceId { get; init; }

    public long? Amount { get; init; }

    public string? ExtRef { get; init; }

    public IReadOnlyList<FiscalItem>? Items { get; init; }
}

public sealed class CancelInvoiceResponse
{
    public string? Status { get; init; }

    public DateTimeOffset? CreatedDate { get; init; }

    public DateTimeOffset? ModifiedDate { get; init; }
}

public sealed class FiscalItem
{
    public required string Name { get; init; }

    public required decimal Qty { get; init; }

    public required long Sum { get; init; }

    public required string Code { get; init; }

    public string? Barcode { get; init; }

    public string? Header { get; init; }

    public string? Footer { get; init; }

    public IReadOnlyList<int>? Tax { get; init; }

    public string? Uktzed { get; init; }
}

public sealed class FinalizeInvoiceRequest
{
    public required string InvoiceId { get; init; }

    public required long Amount { get; init; }

    public IReadOnlyList<FiscalItem>? Items { get; init; }
}

public sealed class FinalizeInvoiceResponse
{
    public string? Status { get; init; }
}

public sealed class ReceiptResponse
{
    public string? File { get; init; }
}

public sealed class FiscalCheck
{
    public string? Id { get; init; }

    public string? Type { get; init; }

    public string? Status { get; init; }

    public string? StatusDescription { get; init; }

    public string? TaxUrl { get; init; }

    public string? File { get; init; }

    public string? FiscalizationSource { get; init; }
}

public sealed class CardData
{
    public required string Pan { get; init; }

    public required string Exp { get; init; }

    public required string Cvv { get; init; }
}

public sealed class PaymentDirectRequest
{
    public required long Amount { get; init; }

    public int? Ccy { get; init; }

    public required CardData CardData { get; init; }

    public MerchantPaymInfo? MerchantPaymInfo { get; init; }

    public string? WebHookUrl { get; init; }

    public string? PaymentType { get; init; }

    public SaveCardData? SaveCardData { get; init; }

    public string? RedirectUrl { get; init; }

    public string? InitiationKind { get; init; }
}

public sealed class PaymentResult
{
    public string? InvoiceId { get; init; }

    public string? TdsUrl { get; init; }

    public string? Status { get; init; }

    public string? FailureReason { get; init; }

    public long Amount { get; init; }

    public int Ccy { get; init; }

    public DateTimeOffset? CreatedDate { get; init; }

    public DateTimeOffset? ModifiedDate { get; init; }
}

public sealed class SyncCardData
{
    public required string Pan { get; init; }

    public required string Type { get; init; }

    public required string Exp { get; init; }

    public string? Cvv { get; init; }

    public required string EciIndicator { get; init; }

    public string? Cavv { get; init; }

    public string? Tavv { get; init; }

    public string? DsTranId { get; init; }

    [System.Text.Json.Serialization.JsonPropertyName("tReqID")]
    public string? TReqId { get; init; }

    public string? Mit { get; init; }

    public decimal? Sst { get; init; }

    public string? Tid { get; init; }
}

public sealed class ApplePayPayload
{
    public required string Token { get; init; }

    public required string Exp { get; init; }

    public required string EciIndicator { get; init; }

    public string? Cryptogram { get; init; }
}

public sealed class GooglePayPayload
{
    public required string Token { get; init; }

    public required string Exp { get; init; }

    public required string EciIndicator { get; init; }

    public string? Cryptogram { get; init; }
}

public sealed class SyncPaymentRequest
{
    public required long Amount { get; init; }

    public required int Ccy { get; init; }

    public MerchantPaymInfo? MerchantPaymInfo { get; init; }

    public SyncCardData? CardData { get; init; }

    public ApplePayPayload? ApplePay { get; init; }

    public GooglePayPayload? GooglePay { get; init; }
}
