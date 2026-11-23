# monobank-acquiring-dotnet

Unofficial .NET SDK for Monobank Acquiring API. Create invoices, use hosted checkout, and verify ECDSA webhooks.

Not affiliated with monobank.

[![NuGet](https://img.shields.io/nuget/v/MonoBankAcquiring.svg?style=flat-square)](https://www.nuget.org/packages/MonoBankAcquiring)
[![NuGet Downloads](https://img.shields.io/nuget/dt/MonoBankAcquiring.svg?style=flat-square)](https://www.nuget.org/packages/MonoBankAcquiring)
[![Build](https://img.shields.io/github/actions/workflow/status/ConstantinOr/monobank-acquiring-dotnet/ci.yml?branch=main&style=flat-square&label=build)](https://github.com/ConstantinOr/monobank-acquiring-dotnet/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg?style=flat-square)](LICENSE)

**Targets:** `netstandard2.0` · `net8.0` · `net10.0` (LTS)

Also covers wallets, subscriptions, split, POS, and T2P — one `MonoAcquiringClient`.

[Українською](#українською) · [English](#english)

<p align="center">
  <img src="docs/social-preview.jpg" alt="monobank-acquiring-dotnet" width="720" />
</p>

---

## English

### Install

```bash
dotnet add package MonoBankAcquiring
```

### ASP.NET DI

```csharp
builder.Services.AddMonoAcquiring(builder.Configuration);
// appsettings: { "MonoBank": { "Token": "..." } }
// or: builder.Services.AddMonoAcquiring(o => o.Token = token);

app.MapPost("/mono/webhook", async (MonoAcquiringClient mono, HttpRequest req) =>
{
    using var ms = new MemoryStream();
    await req.Body.CopyToAsync(ms);
    var body = ms.ToArray();
    var pub = await mono.GetPubKeyAsync();
    if (!req.Headers.TryGetValue("X-Sign", out var sign)
        || string.IsNullOrWhiteSpace(pub.Key)
        || !WebhookVerifier.Verify(body, sign.ToString(), pub.Key))
        return Results.Unauthorized();

    var status = InvoiceStatus.Parse(body);
    return Results.Ok();
});
```

Full sample: `samples/AspNetWebhook` (`dotnet run --project samples/AspNetWebhook`).

### Run in one minute

```bash
export MONOBANK_TOKEN=your_x_token_here   # from https://web.monobank.ua/ or test token
dotnet run --project samples/QuickStart
```

Default is **read-only**: merchant details + webhook public key.  
To create a real invoice:

```bash
dotnet run --project samples/QuickStart -- --create-invoice
```

Or copy `samples/*/appsettings.example.json` → `appsettings.json` (gitignored). Never commit real tokens.

```csharp
using var http = new HttpClient
{
    BaseAddress = new Uri(MonoAcquiringOptions.DefaultBaseAddress)
};
var mono = new MonoAcquiringClient(http, new MonoAcquiringOptions { Token = token });

var invoice = await mono.CreateInvoiceAsync(new CreateInvoiceRequest
{
    Amount = 4200, // minor units: 4200 = 42.00 UAH
    Ccy = 980,
    RedirectUrl = "https://example.com/result",
    WebHookUrl = "https://example.com/mono/webhook",
});

Console.WriteLine(invoice.PageUrl); // pay.mbnk.biz — hosted checkout
```

### Requirements

| | |
|---|---|
| Library TFMs | `netstandard2.0`, `net8.0`, `net10.0` |
| Runs on | .NET Framework 4.6.1+, .NET Core 2.0+, .NET 5+ / .NET 8 / .NET 10 |
| Build / tests | .NET 10 SDK (+ 8 for multi-TFM tests) |
| Auth | Merchant `X-Token` |

On `netstandard2.0` the package uses `System.Text.Json` / `System.Net.Http.Json` plus `Microsoft.Extensions.Http`. On `net8`/`net10` Extensions packages are referenced for DI.

### API surface

| Area | Methods |
|------|---------|
| Invoice | `CreateInvoiceAsync`, `GetInvoiceStatusAsync`, `CancelInvoiceAsync`, `RemoveInvoiceAsync`, `FinalizeInvoiceAsync`, `GetReceiptAsync`, `GetFiscalChecksAsync`, `PaymentDirectAsync`, `SyncPaymentAsync` |
| QR / merchant | `GetQrListAsync`, `GetQrDetailsAsync`, `ResetQrAmountAsync`, `GetMerchantDetailsAsync`, `GetStatementAsync`, `GetEmployeesAsync`, `GetSubmerchantsAsync`, `GetPubKeyAsync` |
| Wallet | `GetWalletAsync`, `WalletPaymentAsync`, `DeleteCardAsync` |
| Subscriptions | `CreateSubscriptionAsync`, `GetSubscriptionStatusAsync`, `GetSubscriptionPaymentsAsync`, `GetSubscriptionsAsync`, `RemoveSubscriptionAsync`, `EditSubscriptionAsync` |
| Split | `GetSplitReceiversAsync`, `GetSplitReceiverAsync` |
| monopay | `ImportMonoPayKeyAsync`, `DeleteMonoPayKeyAsync`, `GetMonoPayKeysAsync`, `MonoPaySigner.Sign` |
| POS | `CancelPosTransactionAsync`, `GetPosCancelStatusAsync` |
| T2P | `GetTerminalsAsync`, `GetT2pPaymentStatusAsync` |
| Webhook | `WebhookVerifier.Verify`, `InvoiceStatus.Parse` |

Base: `https://api.monobank.ua/` · Auth: `X-Token` · Optional: `X-Cms`, `X-Cms-Version`.

This library **receives** payments (acquiring). No personal P2P / Open API currency rates. Closest outbound: refunds, split, tips.

### Webhooks

```csharp
if (!WebhookVerifier.Verify(body, xSign, pubKey.Key))
    return; // reject

var status = InvoiceStatus.Parse(body);
```

### PCI

Hosted `pageUrl` checkout → no card data on your site.  
`PaymentDirectAsync` sends PAN/CVV — only with an active PCI DSS certificate.

### Tests & CI

```bash
dotnet test
```

GitHub Actions builds, tests, and packs on every push; tags `v*` publish a GitHub Release with `.nupkg`.

### Docs

- [Acquiring API](https://monobank.ua/api-docs/acquiring)
- [Invoice create](https://monobank.ua/api-docs/acquiring/methods/ia/post--api--merchant--invoice--create)
- [NuGet](https://www.nuget.org/packages/MonoBankAcquiring)
- [Release v1.0.0](https://github.com/ConstantinOr/monobank-acquiring-dotnet/releases/tag/v1.0.0)

---

## Українською

Неофіційний .NET SDK для [API еквайрингу monobank](https://monobank.ua/api-docs/acquiring). Створення рахунків, hosted checkout і перевірка ECDSA-вебхуків.

Не афілійовано з monobank.

[![NuGet](https://img.shields.io/nuget/v/MonoBankAcquiring.svg?style=flat-square)](https://www.nuget.org/packages/MonoBankAcquiring)
[![NuGet Downloads](https://img.shields.io/nuget/dt/MonoBankAcquiring.svg?style=flat-square)](https://www.nuget.org/packages/MonoBankAcquiring)
[![Build](https://img.shields.io/github/actions/workflow/status/ConstantinOr/monobank-acquiring-dotnet/ci.yml?branch=main&style=flat-square&label=build)](https://github.com/ConstantinOr/monobank-acquiring-dotnet/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg?style=flat-square)](LICENSE)

**Таргети:** `netstandard2.0` · `net8.0` · `net10.0` · **Ліцензія:** MIT

### Встановлення

```bash
dotnet add package MonoBankAcquiring
builder.Services.AddMonoAcquiring(builder.Configuration); // ASP.NET
```

### За хвилину

```bash
export MONOBANK_TOKEN=your_x_token_here
dotnet run --project samples/QuickStart
dotnet run --project samples/AspNetWebhook   # invoice + ECDSA webhook
```

За замовчуванням QuickStart лише merchant info + pubkey.  
Реальний invoice: `-- --create-invoice`. Токен у git не комітьте.

Деталі API — в англійській секції. Документація: [еквайринг](https://monobank.ua/api-docs/acquiring).
