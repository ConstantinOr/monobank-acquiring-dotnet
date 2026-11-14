# monobank-acquiring-dotnet

A full-featured .NET client for the [monobank acquiring API](https://monobank.ua/api-docs/acquiring).  
Create invoices, accept card payments on monobank’s hosted page (no PAN/CVV on your side), verify webhooks, wallets, subscriptions, split, POS, and T2P.

**Targets:** `netstandard2.0` · `net10.0` (LTS) · **License:** MIT

[Українською](#українською) · [English](#english)

<p align="center">
  <img src="docs/social-preview.jpg" alt="monobank-acquiring-dotnet" width="720" />
</p>

---

## English

### Run in one minute

```bash
export MONOBANK_TOKEN=your_x_token_here   # from https://web.monobank.ua/ or test token
dotnet run --project samples/QuickStart
```

Or copy `samples/QuickStart/appsettings.example.json` → `appsettings.json` (gitignored) and set `MonoBank:Token`.

The sample prints merchant details, loads the webhook public key, creates a **42.00 UAH** invoice, and prints `pageUrl` — open it to pay on monobank.

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

Never commit real tokens. Prefer `MONOBANK_TOKEN`.

### Install

```bash
dotnet add reference path/to/src/MonoBank.Acquiring/MonoBank.Acquiring.csproj
```

### Requirements

| | |
|---|---|
| Library TFMs | `netstandard2.0`, `net10.0` |
| Runs on | .NET Framework 4.6.1+, .NET Core 2.0+, .NET 5+ / .NET 10 |
| Build / tests | .NET 10 SDK |
| Auth | Merchant `X-Token` |

On `netstandard2.0` the package uses `System.Text.Json` / `System.Net.Http.Json`. On `net10.0` — BCL only.

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

---

## Українською

Повнофункціональний .NET-клієнт для [API еквайрингу monobank](https://monobank.ua/api-docs/acquiring).  
Рахунки, оплата на сторінці monobank (без PAN/CVV у вас), вебхуки, гаманець, підписки, спліт, POS, T2P.

**Таргети:** `netstandard2.0` · `net10.0` · **Ліцензія:** MIT

### За хвилину

```bash
export MONOBANK_TOKEN=your_x_token_here
dotnet run --project samples/QuickStart
```

Або скопіюйте `appsettings.example.json` → `appsettings.json` (у `.gitignore`) і вкажіть токен. Токен у git не комітьте.

### Підключення / тести

```bash
dotnet add reference path/to/src/MonoBank.Acquiring/MonoBank.Acquiring.csproj
dotnet test
```

Деталі API — у англійській секції вище. Документація monobank: [еквайринг](https://monobank.ua/api-docs/acquiring).
