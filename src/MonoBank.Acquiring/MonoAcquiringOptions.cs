namespace MonoBank.Acquiring;

public sealed class MonoAcquiringOptions
{
    public const string DefaultBaseAddress = "https://api.monobank.ua/";

    /// <summary>Configuration section name for <c>AddMonoAcquiring(configuration)</c>.</summary>
    public const string SectionName = "MonoBank";

    public string Token { get; set; } = "";

    public string? Cms { get; set; }

    public string? CmsVersion { get; set; }

    public Uri BaseAddress { get; set; } = new(DefaultBaseAddress);
}
