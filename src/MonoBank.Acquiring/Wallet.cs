namespace MonoBank.Acquiring;

public sealed partial class MonoAcquiringClient
{
    public async Task<IReadOnlyList<WalletCard>> GetWalletAsync(string? walletId = null, CancellationToken cancellationToken = default)
    {
        var page = await SendAsync<WalletResponse>(
            HttpMethod.Get,
            "api/merchant/wallet",
            null,
            cancellationToken,
            [Q("walletId", walletId)]).ConfigureAwait(false);
        return page.Wallet ?? [];
    }

    public Task<PaymentResult> WalletPaymentAsync(WalletPaymentRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        RequireId(request.CardToken, nameof(request.CardToken));
        RequireId(request.InitiationKind, nameof(request.InitiationKind));
        return SendAsync<PaymentResult>(HttpMethod.Post, "api/merchant/wallet/payment", request, cancellationToken);
    }

    public Task DeleteCardAsync(string cardToken, CancellationToken cancellationToken = default)
    {
        RequireId(cardToken, nameof(cardToken));
        return SendAsync(HttpMethod.Delete, "api/merchant/wallet/card", null, cancellationToken, [Q("cardToken", cardToken)]);
    }

    private sealed class WalletResponse
    {
        public IReadOnlyList<WalletCard>? Wallet { get; set; }
    }
}

public sealed class WalletCard
{
    public string? CardToken { get; init; }

    public string? MaskedPan { get; init; }

    public string? Country { get; init; }
}

public sealed class WalletPaymentRequest
{
    public required string CardToken { get; init; }

    public required long Amount { get; init; }

    public required int Ccy { get; init; }

    public string? RedirectUrl { get; init; }

    public string? WebHookUrl { get; init; }

    public required string InitiationKind { get; init; }

    public MerchantPaymInfo? MerchantPaymInfo { get; init; }

    public string? PaymentType { get; init; }
}
