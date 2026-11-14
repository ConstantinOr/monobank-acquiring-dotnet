namespace MonoBank.Acquiring;

public sealed partial class MonoAcquiringClient
{
    public Task<CreateSubscriptionResponse> CreateSubscriptionAsync(CreateSubscriptionRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        RequireId(request.Interval, nameof(request.Interval));
        return SendAsync<CreateSubscriptionResponse>(HttpMethod.Post, "api/merchant/subscription/create", request, cancellationToken);
    }

    public Task<SubscriptionStatus> GetSubscriptionStatusAsync(string subscriptionId, CancellationToken cancellationToken = default)
    {
        RequireId(subscriptionId, nameof(subscriptionId));
        return SendAsync<SubscriptionStatus>(
            HttpMethod.Get,
            "api/merchant/subscription/status",
            null,
            cancellationToken,
            [Q("subscriptionId", subscriptionId)]);
    }

    public Task<SubscriptionPaymentsPage> GetSubscriptionPaymentsAsync(
        string subscriptionId,
        DateTimeOffset dateFrom,
        DateTimeOffset? dateTo = null,
        int? limit = null,
        int? page = null,
        CancellationToken cancellationToken = default)
    {
        RequireId(subscriptionId, nameof(subscriptionId));
        return SendAsync<SubscriptionPaymentsPage>(
            HttpMethod.Get,
            "api/merchant/subscription/payments",
            null,
            cancellationToken,
            [
                Q("subscriptionId", subscriptionId),
                Q("dateFrom", dateFrom),
                Q("dateTo", dateTo),
                Q("limit", limit),
                Q("page", page),
            ]);
    }

    public Task<SubscriptionListPage> GetSubscriptionsAsync(
        DateTimeOffset dateFrom,
        DateTimeOffset? dateTo = null,
        string? status = null,
        int? limit = null,
        int? page = null,
        CancellationToken cancellationToken = default) =>
        SendAsync<SubscriptionListPage>(
            HttpMethod.Get,
            "api/merchant/subscription/list",
            null,
            cancellationToken,
            [
                Q("dateFrom", dateFrom),
                Q("dateTo", dateTo),
                Q("status", status),
                Q("limit", limit),
                Q("page", page),
            ]);

    public Task RemoveSubscriptionAsync(string subscriptionId, CancellationToken cancellationToken = default)
    {
        RequireId(subscriptionId, nameof(subscriptionId));
        return SendAsync(HttpMethod.Post, "api/merchant/subscription/remove", new SubscriptionIdBody(subscriptionId), cancellationToken);
    }

    public Task EditSubscriptionAsync(EditSubscriptionRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        RequireId(request.SubscriptionId, nameof(request.SubscriptionId));
        RequireId(request.Action, nameof(request.Action));
        return SendAsync(HttpMethod.Post, "api/merchant/subscription/edit", request, cancellationToken);
    }

    private sealed class SubscriptionIdBody(string subscriptionId)
    {
        public string SubscriptionId { get; } = subscriptionId;
    }
}

public sealed class CreateSubscriptionRequest
{
    public required long Amount { get; init; }

    public int? Ccy { get; init; }

    public string? RedirectUrl { get; init; }

    public SubscriptionWebHooks? WebHookUrls { get; init; }

    public required string Interval { get; init; }

    public int? Validity { get; init; }
}

public sealed class SubscriptionWebHooks
{
    public string? ChargeUrl { get; init; }

    public string? StatusUrl { get; init; }
}

public sealed class CreateSubscriptionResponse
{
    public string? SubscriptionId { get; init; }

    public string? PageUrl { get; init; }
}

public sealed class SubscriptionStatus
{
    public string? SubscriptionId { get; init; }

    public string? Status { get; init; }

    public string? StartDate { get; init; }

    public string? EndDate { get; init; }

    public long Amount { get; init; }

    public int Ccy { get; init; }

    public string? Interval { get; init; }

    public string? NextChargeDate { get; init; }

    public string? CancellationDesc { get; init; }

    public SubscriptionSummary? Summary { get; init; }

    public SubscriptionWallet? WalletData { get; init; }
}

public sealed class SubscriptionSummary
{
    public int TotalPaid { get; init; }

    public int TotalFailed { get; init; }
}

public sealed class SubscriptionWallet
{
    public string? CardToken { get; init; }

    public string? WalletId { get; init; }

    public string? Status { get; init; }

    public string? FailureDescription { get; init; }
}

public sealed class SubscriptionListPage
{
    public IReadOnlyList<SubscriptionItem>? List { get; init; }

    public PageInfo? Pagination { get; init; }
}

public sealed class SubscriptionItem
{
    public string? SubscriptionId { get; init; }

    public long Amount { get; init; }

    public string? Interval { get; init; }

    public string? StartDate { get; init; }

    public string? Created { get; init; }

    public string? NextChargeDate { get; init; }

    public string? EndDate { get; init; }

    public string? Status { get; init; }
}

public sealed class SubscriptionPaymentsPage
{
    public IReadOnlyList<SubscriptionPayment>? Payments { get; init; }

    public PageInfo? Pagination { get; init; }
}

public sealed class SubscriptionPayment
{
    public long Amount { get; init; }

    public int Ccy { get; init; }

    public string? Status { get; init; }

    public string? ChargedAt { get; init; }
}

public sealed class PageInfo
{
    public int TotalItems { get; init; }

    public int ItemsPerPage { get; init; }

    public int CurrentPage { get; init; }

    public int TotalPages { get; init; }
}

public sealed class EditSubscriptionRequest
{
    public required string SubscriptionId { get; init; }

    public required string Action { get; init; }

    public long? RefundAmount { get; init; }
}
