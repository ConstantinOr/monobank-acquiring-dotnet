namespace MonoBank.Acquiring;

public sealed partial class MonoAcquiringClient
{
    public async Task<string?> ImportMonoPayKeyAsync(ImportMonoPayKeyRequest request, CancellationToken cancellationToken = default)
    {
        Guard.NotNull(request);
        RequireId(request.KeyValue, nameof(request.KeyValue));
        var response = await SendAsync<MonoPayImportResponse>(HttpMethod.Post, "api/merchant/monopay/pubkey-import", request, cancellationToken).ConfigureAwait(false);
        return response.Result?.KeyId;
    }

    public Task DeleteMonoPayKeyAsync(string keyId, CancellationToken cancellationToken = default)
    {
        RequireId(keyId, nameof(keyId));
        return SendAsync(HttpMethod.Post, "api/merchant/monopay/pubkey-delete", new MonoPayKeyIdBody(keyId), cancellationToken);
    }

    public async Task<IReadOnlyList<MonoPayKey>> GetMonoPayKeysAsync(CancellationToken cancellationToken = default)
    {
        var page = await SendAsync<MonoPayKeyListResponse>(HttpMethod.Get, "api/merchant/monopay/pubkey-list", null, cancellationToken).ConfigureAwait(false);
        return page.Result ?? [];
    }

    private sealed class MonoPayKeyIdBody(string keyId)
    {
        public string KeyId { get; } = keyId;
    }

    private sealed class MonoPayImportResponse
    {
        public MonoPayKeyIdBody? Result { get; set; }
    }

    private sealed class MonoPayKeyListResponse
    {
        public IReadOnlyList<MonoPayKey>? Result { get; set; }
    }
}

public sealed class ImportMonoPayKeyRequest
{
    public required string KeyValue { get; init; }

    public string? KeyName { get; init; }

    public string? ExpiresAt { get; init; }
}

public sealed class MonoPayKey
{
    public string? KeyId { get; init; }

    public string? KeyValue { get; init; }

    public string? KeyName { get; init; }

    public string? ExpiresAt { get; init; }
}
