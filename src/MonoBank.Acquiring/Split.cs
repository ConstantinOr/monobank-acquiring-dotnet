namespace MonoBank.Acquiring;

public sealed partial class MonoAcquiringClient
{
    public async Task<IReadOnlyList<SplitReceiver>> GetSplitReceiversAsync(CancellationToken cancellationToken = default)
    {
        var page = await SendAsync<SplitReceiverListResponse>(HttpMethod.Get, "api/merchant/split-receiver/list", null, cancellationToken).ConfigureAwait(false);
        return page.List ?? [];
    }

    public Task<SplitReceiver> GetSplitReceiverAsync(string splitReceiverId, CancellationToken cancellationToken = default)
    {
        RequireId(splitReceiverId, nameof(splitReceiverId));
        return SendAsync<SplitReceiver>(
            HttpMethod.Get,
            "api/merchant/split-receiver/get-by-id",
            null,
            cancellationToken,
            [Q("splitReceiverId", splitReceiverId)]);
    }

    private sealed class SplitReceiverListResponse
    {
        public IReadOnlyList<SplitReceiver>? List { get; set; }
    }
}

public sealed class SplitReceiver
{
    public string? SplitReceiverId { get; init; }

    public string? Okpo { get; init; }

    public string? Name { get; init; }
}
