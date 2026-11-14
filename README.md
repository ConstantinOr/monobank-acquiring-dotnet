# MonoBank.Acquiring

Full-featured [.NET](https://dotnet.microsoft.com/) client for the complete [monobank acquiring API](https://monobank.ua/api-docs/acquiring).

**Targets:** `netstandard2.0` · `net10.0` (LTS)

Invoices, QR, wallet/tokenization, subscriptions, split, monopay, POS, T2P, fiscal checks, statements, webhook verification — one `MonoAcquiringClient`, not an invoice-only wrapper.

[Українською](#monobankacquiring-українською)

---

## English

### Requirements

| | |
|---|---|
| Library TFMs | `netstandard2.0`, `net10.0` |
| Runs on | .NET Framework 4.6.1+, .NET Core 2.0+, .NET 5+ / .NET 10 |
| Build / tests | .NET 10 SDK |
| Auth | Merchant `X-Token` from [web.monobank.ua](https://web.monobank.ua/) (or test token from [api.monobank.ua](https://api.monobank.ua/)) |

On `netstandard2.0` the package pulls `System.Text.Json` / `System.Net.Http.Json`. On `net10.0` — BCL only.

### Install

```bash
dotnet add reference path/to/src/MonoBank.Acquiring/MonoBank.Acquiring.csproj
```

Repo: https://github.com/ConstantinOr/MonoBank.Acquiring

### Quick start

Amounts are **minor units** (`long`): `4200` = `42.00 UAH`.

```csharp
using System;
using System.Net.Http;
using System.Threading.Tasks;
using MonoBank.Acquiring;

var http = new HttpClient
{
    BaseAddress = new Uri(MonoAcquiringOptions.DefaultBaseAddress)
};
var mono = new MonoAcquiringClient(http, new MonoAcquiringOptions
{
    Token = token
});

var invoice = await mono.CreateInvoiceAsync(new CreateInvoiceRequest
{
    Amount = 4200,
    Ccy = 980,
    RedirectUrl = "https://example.com/result",
    WebHookUrl = "https://example.com/mono/webhook",
    MerchantPaymInfo = new MerchantPaymInfo
    {
        Reference = "order-15",
        Destination = "Order #15",
        BasketOrder = new[]
        {
            new BasketItem
            {
                Name = "Item",
                Qty = 2,
                Sum = 2100,
                Total = 4200,
                Code = "sku-1",
            },
        },
    },
});

// Redirect the buyer to invoice.PageUrl
```

Prefer `IHttpClientFactory` in production.

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

- Base URL: `https://api.monobank.ua/`
- Auth: `X-Token`
- Optional: `X-Cms`, `X-Cms-Version`

This library **receives** payments (acquiring). No personal P2P. Closest outbound flows: refunds (`CancelInvoiceAsync`), split receivers, tips (`TipsEmployeeId`).

### Errors

Non-success → `MonoAcquiringException` with `StatusCode`, `ErrCode`, `ErrText`, raw `Body`.

### Webhooks

Verify `X-Sign` on the **raw** body bytes before parsing. Public key: `GetPubKeyAsync` (`key` = base64 of a PEM SPKI key).

```csharp
if (!WebhookVerifier.Verify(body, xSign, pubKey.Key))
{
    // reject
    return;
}

var status = InvoiceStatus.Parse(body);
```

### monopay button signature

```csharp
var signature = MonoPaySigner.Sign(orderJson, requestId, privateKeyPem);
// UTF-8(orderJson + requestId), ECDSA P-256 / SHA-256, DER, base64
```

### PCI

`PaymentDirectAsync` sends PAN/CVV — only with an active PCI DSS certificate.

### Tests

```bash
dotnet test
```

Fake `HttpMessageHandler` only — no live monobank calls.

### Docs

- [Acquiring API](https://monobank.ua/api-docs/acquiring)
- [Invoice create](https://monobank.ua/api-docs/acquiring/methods/ia/post--api--merchant--invoice--create)

---

## MonoBank.Acquiring (українською)

Повнофункціональний [.NET](https://dotnet.microsoft.com/)-клієнт для повного [API еквайрингу monobank](https://monobank.ua/api-docs/acquiring).

**Таргети:** `netstandard2.0` · `net10.0` (LTS)

Рахунки, QR, гаманець/токенізація, підписки, спліт, monopay, POS, T2P, фіскальні чеки, виписка, перевірка вебхуків — один `MonoAcquiringClient`, не обрізана обгортка над create invoice.

### Вимоги

| | |
|---|---|
| TFM бібліотеки | `netstandard2.0`, `net10.0` |
| Працює на | .NET Framework 4.6.1+, .NET Core 2.0+, .NET 5+ / .NET 10 |
| Збірка / тести | .NET 10 SDK |
| Авторизація | Merchant `X-Token` з [web.monobank.ua](https://web.monobank.ua/) (або тест з [api.monobank.ua](https://api.monobank.ua/)) |

На `netstandard2.0` підтягуються `System.Text.Json` / `System.Net.Http.Json`. На `net10.0` — лише BCL.

### Підключення

```bash
dotnet add reference path/to/src/MonoBank.Acquiring/MonoBank.Acquiring.csproj
```

Репозиторій: https://github.com/ConstantinOr/MonoBank.Acquiring

### Швидкий старт

Суми — **мінімальні одиниці** (`long`): `4200` = `42.00 грн`.

```csharp
using System;
using System.Net.Http;
using System.Threading.Tasks;
using MonoBank.Acquiring;

var http = new HttpClient
{
    BaseAddress = new Uri(MonoAcquiringOptions.DefaultBaseAddress)
};
var mono = new MonoAcquiringClient(http, new MonoAcquiringOptions
{
    Token = token
});

var invoice = await mono.CreateInvoiceAsync(new CreateInvoiceRequest
{
    Amount = 4200,
    Ccy = 980,
    RedirectUrl = "https://example.com/result",
    WebHookUrl = "https://example.com/mono/webhook",
    MerchantPaymInfo = new MerchantPaymInfo
    {
        Reference = "order-15",
        Destination = "Замовлення №15",
        BasketOrder = new[]
        {
            new BasketItem
            {
                Name = "Товар",
                Qty = 2,
                Sum = 2100,
                Total = 4200,
                Code = "sku-1",
            },
        },
    },
});

// Переадресуйте покупця на invoice.PageUrl
```

У проді краще `IHttpClientFactory`.

### Покриття API

| Зона | Методи |
|------|--------|
| Рахунки | `CreateInvoiceAsync`, `GetInvoiceStatusAsync`, `CancelInvoiceAsync`, `RemoveInvoiceAsync`, `FinalizeInvoiceAsync`, `GetReceiptAsync`, `GetFiscalChecksAsync`, `PaymentDirectAsync`, `SyncPaymentAsync` |
| QR / мерчант | `GetQrListAsync`, `GetQrDetailsAsync`, `ResetQrAmountAsync`, `GetMerchantDetailsAsync`, `GetStatementAsync`, `GetEmployeesAsync`, `GetSubmerchantsAsync`, `GetPubKeyAsync` |
| Гаманець | `GetWalletAsync`, `WalletPaymentAsync`, `DeleteCardAsync` |
| Підписки | `CreateSubscriptionAsync`, `GetSubscriptionStatusAsync`, `GetSubscriptionPaymentsAsync`, `GetSubscriptionsAsync`, `RemoveSubscriptionAsync`, `EditSubscriptionAsync` |
| Розщеплення | `GetSplitReceiversAsync`, `GetSplitReceiverAsync` |
| monopay | `ImportMonoPayKeyAsync`, `DeleteMonoPayKeyAsync`, `GetMonoPayKeysAsync`, `MonoPaySigner.Sign` |
| POS | `CancelPosTransactionAsync`, `GetPosCancelStatusAsync` |
| T2P | `GetTerminalsAsync`, `GetT2pPaymentStatusAsync` |
| Webhook | `WebhookVerifier.Verify`, `InvoiceStatus.Parse` |

- База: `https://api.monobank.ua/`
- Авторизація: `X-Token`
- Опційно: `X-Cms`, `X-Cms-Version`

Бібліотека **приймає** оплату (еквайринг). Особистих P2P немає. Найближче «вихідне»: повернення (`CancelInvoiceAsync`), спліт-отримувачі, чайові (`TipsEmployeeId`).

### Помилки

Невдалі відповіді → `MonoAcquiringException`: `StatusCode`, `ErrCode`, `ErrText`, сирий `Body`.

### Вебхуки

Перевіряйте `X-Sign` по **сирих** байтах тіла до парсингу. Ключ — `GetPubKeyAsync` (`key` = base64 від PEM SPKI).

```csharp
if (!WebhookVerifier.Verify(body, xSign, pubKey.Key))
{
    // відхилити
    return;
}

var status = InvoiceStatus.Parse(body);
```

### Підпис кнопки monopay

```csharp
var signature = MonoPaySigner.Sign(orderJson, requestId, privateKeyPem);
// UTF-8(orderJson + requestId), ECDSA P-256 / SHA-256, DER, base64
```

### PCI

`PaymentDirectAsync` передає PAN/CVV — лише з активним PCI DSS.

### Тести

```bash
dotnet test
```

Лише фейковий `HttpMessageHandler` — живий monobank не викликається.

### Документація

- [Еквайринг](https://monobank.ua/api-docs/acquiring)
- [Створення рахунку](https://monobank.ua/api-docs/acquiring/methods/ia/post--api--merchant--invoice--create)
