using System.Text.Json;
using MonoBank.Acquiring;

// Token: env MONOBANK_TOKEN (preferred) or samples/QuickStart/appsettings.json (gitignored).
// Get a merchant token at https://web.monobank.ua/ or a test token at https://api.monobank.ua/
//
// Default: read-only merchant info + pubkey.
// Create a real invoice only with: dotnet run --project samples/QuickStart -- --create-invoice

var createInvoice = args.Any(a =>
    string.Equals(a, "--create-invoice", StringComparison.OrdinalIgnoreCase)
    || string.Equals(a, "-c", StringComparison.OrdinalIgnoreCase));

var token = Environment.GetEnvironmentVariable("MONOBANK_TOKEN");
if (string.IsNullOrWhiteSpace(token))
    token = TryReadTokenFromAppSettings();

if (string.IsNullOrWhiteSpace(token) || token.Contains("PUT_YOUR", StringComparison.Ordinal))
{
    Console.Error.WriteLine(
        """
        Missing merchant X-Token.

          export MONOBANK_TOKEN=your_token_here
          dotnet run --project samples/QuickStart

        Optional (creates a real invoice):
          dotnet run --project samples/QuickStart -- --create-invoice

        Or copy appsettings.example.json → appsettings.json and set MonoBank:Token.
        """);
    return 1;
}

using var http = new HttpClient
{
    BaseAddress = new Uri(MonoAcquiringOptions.DefaultBaseAddress),
};
var mono = new MonoAcquiringClient(http, new MonoAcquiringOptions
{
    Token = token,
    Cms = "MonoBank.Acquiring.QuickStart",
    CmsVersion = "1.0.0",
});

Console.WriteLine("Merchant details…");
var merchant = await mono.GetMerchantDetailsAsync();
Console.WriteLine($"  id   = {merchant.MerchantId}");
Console.WriteLine($"  name = {merchant.MerchantName}");
Console.WriteLine($"  edrpou = {merchant.Edrpou}");

Console.WriteLine();
Console.WriteLine("Webhook public key…");
var pub = await mono.GetPubKeyAsync();
Console.WriteLine($"  key length = {pub.Key?.Length ?? 0} chars (base64 PEM)");

if (!createInvoice)
{
    Console.WriteLine();
    Console.WriteLine("Done (read-only). To create a real invoice:");
    Console.WriteLine("  dotnet run --project samples/QuickStart -- --create-invoice");
    return 0;
}

Console.WriteLine();
Console.WriteLine("Creating invoice (42.00 UAH)…");
var invoice = await mono.CreateInvoiceAsync(new CreateInvoiceRequest
{
    Amount = 4200,
    Ccy = 980,
    RedirectUrl = "https://example.com/result",
    WebHookUrl = "https://example.com/mono/webhook",
    MerchantPaymInfo = new MerchantPaymInfo
    {
        Reference = $"quickstart-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}",
        Destination = "MonoBank.Acquiring QuickStart",
    },
});

Console.WriteLine($"  invoiceId = {invoice.InvoiceId}");
Console.WriteLine($"  pageUrl   = {invoice.PageUrl}");
Console.WriteLine();
Console.WriteLine("Open pageUrl in a browser to pay on monobank's hosted page (no card data on your side).");
return 0;

static string? TryReadTokenFromAppSettings()
{
    var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
    if (!File.Exists(path))
        path = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
    if (!File.Exists(path))
        return null;

    using var doc = JsonDocument.Parse(File.ReadAllText(path));
    if (doc.RootElement.TryGetProperty("MonoBank", out var mono)
        && mono.TryGetProperty("Token", out var token))
    {
        return token.GetString();
    }

    return null;
}
