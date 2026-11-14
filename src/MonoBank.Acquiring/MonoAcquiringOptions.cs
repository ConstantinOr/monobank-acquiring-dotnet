namespace MonoBank.Acquiring;

public sealed class MonoAcquiringOptions
{
    public const string DefaultBaseAddress = "https://api.monobank.ua/";

    public required string Token { get; init; }

    public string? Cms { get; init; }

    public string? CmsVersion { get; init; }

    public Uri BaseAddress { get; init; } = new(DefaultBaseAddress);
}
