# MonoBank.Acquiring

Full-featured .NET 10 (LTS) client for the complete [monobank acquiring API](https://monobank.ua/api-docs/acquiring): invoices, QR, wallet/tokenization, subscriptions, split, monopay, POS, T2P, fiscal checks, statements, and webhook verification. No third-party dependencies.

[Українською](#monobankacquiring-українською) · [English](#monobankacquiring)

---

## MonoBank.Acquiring

Covers the full acquiring surface documented by monobank (internet / QR / in-app / subscriptions / split / tokenization / marketplace / POS / T2P). One `MonoAcquiringClient` — not a partial invoice wrapper.

### Requirements

- .NET 10 SDK
- Merchant `X-Token` from [web.monobank.ua](https://web.monobank.ua/) or a test token from [api.monobank.ua](https://api.monobank.ua/)

### Install

```bash
dotnet add reference path/to/src/MonoBank.Acquiring/MonoBank.Acquiring.csproj
```

### Quick start

Amounts are **minor units** (`long`): `4200` = `42.00 UAH`.

```csharp
using MonoBank.Acquiring;

var http = new HttpClient { BaseAddress = new Uri(MonoAcquiringOptions.DefaultBaseAddress) };
var mono = new MonoAcquiringClient(http, new MonoAcquiringOptions { Token = token });

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
        BasketOrder =
        [
            new BasketItem
            {
                Name = "Item",
                Qty = 2,
                Sum = 2100,
                Total = 4200,
                Code = "sku-1",
            },
        ],
    },
});

// Redirect the buyer to invoice.PageUrl
```

Prefer `IHttpClientFactory` and inject `HttpClient` in production.

### Full API surface

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

Base URL: `https://api.monobank.ua/`. Auth header: `X-Token`. Optional: `X-Cms`, `X-Cms-Version`.

This library **receives** payments (acquiring). It does not do personal P2P transfers. Closest outbound flows: refunds (`CancelInvoiceAsync`), split receivers, tips (`TipsEmployeeId`).

### Errors

Non-success responses throw `MonoAcquiringException` with `StatusCode`, `ErrCode`, `ErrText`, and raw `Body`.

### Webhooks

Verify `X-Sign` against the raw request body **before** parsing. Public key comes from `GetPubKeyAsync` (`key` is base64 of a PEM SPKI key).

```csharp
if (!WebhookVerifier.Verify(body, xSign, pubKey.Key!))
    return Results.Unauthorized();

var status = InvoiceStatus.Parse(body);
```

### monopay button signature

```csharp
var signature = MonoPaySigner.Sign(orderJson, requestId, privateKeyPem);
// Signs UTF-8 of (orderJson + requestId) with ECDSA P-256 / SHA-256, DER, base64
```

### PCI note

`PaymentDirectAsync` sends PAN/CVV. Only use it if your merchant has an active PCI DSS certificate.

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

Повнофункціональний клієнт під .NET 10 (LTS) для повного [API еквайрингу monobank](https://monobank.ua/api-docs/acquiring): рахунки, QR, гаманець/токенізація, підписки, розщеплення, monopay, POS, T2P, фіскальні чеки, виписка та перевірка вебхуків. Без сторонніх залежностей.

Покриває весь documented acquiring surface (інтернет / QR / in-app / підписки / спліт / токенізація / маркетплейс / POS / T2P). Один `MonoAcquiringClient` — не обрізана обгортка лише над create invoice.

### Вимоги

- .NET 10 SDK
- Merchant-токен `X-Token` з [web.monobank.ua](https://web.monobank.ua/) або тестовий з [api.monobank.ua](https://api.monobank.ua/)

### Підключення

```bash
dotnet add reference path/to/src/MonoBank.Acquiring/MonoBank.Acquiring.csproj
```

### Швидкий старт

Суми — **мінімальні одиниці** (`long`): `4200` = `42.00 грн`.

```csharp
using MonoBank.Acquiring;

var http = new HttpClient { BaseAddress = new Uri(MonoAcquiringOptions.DefaultBaseAddress) };
var mono = new MonoAcquiringClient(http, new MonoAcquiringOptions { Token = token });

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
        BasketOrder =
        [
            new BasketItem
            {
                Name = "Товар",
                Qty = 2,
                Sum = 2100,
                Total = 4200,
                Code = "sku-1",
            },
        ],
    },
});

// Переадресуйте покупця на invoice.PageUrl
```

У проді краще віддавати `HttpClient` через `IHttpClientFactory`.

### Повне покриття API

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

База: `https://api.monobank.ua/`. Авторизація: `X-Token`. Опційно: `X-Cms`, `X-Cms-Version`.

Бібліотека **приймає** оплату (еквайринг). Особистих P2P-переказів тут немає. Найближче «вихідне»: повернення (`CancelInvoiceAsync`), спліт-отримувачі, чайові (`TipsEmployeeId`).

### Помилки

Невдалі відповіді → `MonoAcquiringException` зі `StatusCode`, `ErrCode`, `ErrText` і сирим `Body`.

### Вебхуки

Перевіряйте `X-Sign` по сирих байтах тіла **до** парсингу. Ключ — `GetPubKeyAsync` (поле `key` = base64 від PEM SPKI).

```csharp
if (!WebhookVerifier.Verify(body, xSign, pubKey.Key!))
    return Results.Unauthorized();

var status = InvoiceStatus.Parse(body);
```

### Підпис кнопки monopay

```csharp
var signature = MonoPaySigner.Sign(orderJson, requestId, privateKeyPem);
// Підписує UTF-8 (orderJson + requestId), ECDSA P-256 / SHA-256, DER, base64
```

### PCI

`PaymentDirectAsync` передає PAN/CVV. Потрібен активний PCI DSS у мерчанта.

### Тести

```bash
dotnet test
```

Лише фейковий `HttpMessageHandler` — живий monobank не викликається.

### Документація

- [Еквайринг](https://monobank.ua/api-docs/acquiring)
- [Створення рахунку](https://monobank.ua/api-docs/acquiring/methods/ia/post--api--merchant--invoice--create)
