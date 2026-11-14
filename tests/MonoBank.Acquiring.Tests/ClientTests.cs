using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;

namespace MonoBank.Acquiring.Tests;

public class ClientTests
{
    private const string Token = "test-token";

    [Fact]
    public async Task CreateInvoice_sends_basket_and_omits_nulls()
    {
        var cap = await Call(
            c => c.CreateInvoiceAsync(new CreateInvoiceRequest
            {
                Amount = 4200,
                Ccy = 980,
                MerchantPaymInfo = new MerchantPaymInfo
                {
                    Reference = "ref-1",
                    Metadata = new Dictionary<string, string> { ["order"] = "15" },
                    BasketOrder =
                    [
                        new BasketItem
                        {
                            Name = "Табуретка",
                            Qty = 2,
                            Sum = 2100,
                            Total = 4200,
                            Code = "sku",
                            SplitReceiverId = "recv",
                        },
                    ],
                },
                PaymentType = PaymentType.Hold,
                WithAppUrl = true,
            }),
            """{"invoiceId":"p2_9ZgpZVsl3","pageUrl":"https://pay.mbnk.biz/p2_9ZgpZVsl3","appUrl":"https://mbnk.app/p2_9ZgpZVsl3"}""");

        Assert.Equal(HttpMethod.Post, cap.Method);
        Assert.Equal("/api/merchant/invoice/create", cap.Path);
        Assert.Equal(Token, cap.Header("X-Token"));
        var body = JsonNode.Parse(cap.Body)!;
        Assert.Equal(4200, body["amount"]!.GetValue<int>());
        Assert.Equal("hold", body["paymentType"]!.GetValue<string>());
        Assert.True(body["withAppUrl"]!.GetValue<bool>());
        Assert.Equal("recv", body["merchantPaymInfo"]!["basketOrder"]![0]!["splitReceiverId"]!.GetValue<string>());
        Assert.Equal("15", body["merchantPaymInfo"]!["metadata"]!["order"]!.GetValue<string>());
        Assert.Null(body["redirectUrl"]);
        Assert.Null(body["qrId"]);
        Assert.Null(body["saveCardData"]);

        var created = (CreateInvoiceResponse)cap.Result!;
        Assert.Equal("p2_9ZgpZVsl3", created.InvoiceId);
        Assert.Equal("https://mbnk.app/p2_9ZgpZVsl3", created.AppUrl);
    }

    [Fact]
    public async Task GetInvoiceStatus_reads_payment_and_wallet()
    {
        var cap = await Call(
            c => c.GetInvoiceStatusAsync("p2_9ZgpZVsl3"),
            """
            {
              "invoiceId":"p2_9ZgpZVsl3",
              "status":"success",
              "amount":4200,
              "ccy":980,
              "finalAmount":4200,
              "createdDate":"2019-08-24T14:15:22Z",
              "modifiedDate":"2019-08-24T14:15:22Z",
              "reference":"ref",
              "destination":"Покупка щастя",
              "paymentInfo":{"maskedPan":"444403******1902","paymentSystem":"visa","paymentMethod":"pan","terminal":"MI001088","fee":10,"agentFee":1},
              "walletData":{"cardToken":"tok","walletId":"wal","status":"created"},
              "tipsInfo":{"employeeId":"emp","amount":100},
              "cancelList":[{"status":"success","amount":100,"ccy":980,"extRef":"e1"}]
            }
            """);

        Assert.Equal(HttpMethod.Get, cap.Method);
        Assert.Equal("/api/merchant/invoice/status", cap.Path);
        Assert.Equal("?invoiceId=p2_9ZgpZVsl3", cap.Query);
        var status = (InvoiceStatus)cap.Result!;
        Assert.Equal(InvoiceState.Success, status.Status);
        Assert.Equal(4200, status.Amount);
        Assert.Equal("visa", status.PaymentInfo!.PaymentSystem);
        Assert.Equal("tok", status.WalletData!.CardToken);
        Assert.Equal("e1", status.CancelList![0].ExtRef);
        Assert.Equal(new DateTimeOffset(2019, 8, 24, 14, 15, 22, TimeSpan.Zero), status.ModifiedDate);
    }

    [Fact]
    public async Task Cancel_remove_finalize_receipt_fiscal()
    {
        var cancel = await Call(
            c => c.CancelInvoiceAsync(new CancelInvoiceRequest
            {
                InvoiceId = "p2",
                Amount = 500,
                ExtRef = "ext",
                Items = [new FiscalItem { Name = "Табуретка", Qty = 1, Sum = 500, Code = "sku" }],
            }),
            """{"status":"processing","createdDate":"2023-01-01T00:00:00Z","modifiedDate":"2023-01-01T00:00:00Z"}""");
        Assert.Equal(HttpMethod.Post, cancel.Method);
        Assert.Equal("/api/merchant/invoice/cancel", cancel.Path);
        Assert.Equal("p2", JsonNode.Parse(cancel.Body)!["invoiceId"]!.GetValue<string>());
        Assert.Equal("processing", ((CancelInvoiceResponse)cancel.Result!).Status);

        var remove = await Call(c => c.RemoveInvoiceAsync("p2"), "");
        Assert.Equal("/api/merchant/invoice/remove", remove.Path);
        Assert.Equal("p2", JsonNode.Parse(remove.Body)!["invoiceId"]!.GetValue<string>());

        var finalize = await Call(
            c => c.FinalizeInvoiceAsync(new FinalizeInvoiceRequest { InvoiceId = "p2", Amount = 4200 }),
            """{"status":"success"}""");
        Assert.Equal("/api/merchant/invoice/finalize", finalize.Path);
        Assert.Equal(4200, JsonNode.Parse(finalize.Body)!["amount"]!.GetValue<int>());

        var receipt = await Call(c => c.GetReceiptAsync("p2", "a@b.c"), """{"file":"cGRm"}""");
        Assert.Equal("/api/merchant/invoice/receipt", receipt.Path);
        Assert.Contains("invoiceId=p2", receipt.Query);
        Assert.Contains("email=a%40b.c", receipt.Query);
        Assert.Equal("cGRm", ((ReceiptResponse)receipt.Result!).File);

        var fiscal = await Call(
            c => c.GetFiscalChecksAsync("p2"),
            """{"checks":[{"id":"1","type":"sale","status":"done","fiscalizationSource":"checkbox"}]}""");
        Assert.Equal("/api/merchant/invoice/fiscal-checks", fiscal.Path);
        Assert.Equal("checkbox", ((IReadOnlyList<FiscalCheck>)fiscal.Result!)[0].FiscalizationSource);
    }

    [Fact]
    public async Task PaymentDirect_and_sync_payment()
    {
        var direct = await Call(
            c => c.PaymentDirectAsync(new PaymentDirectRequest
            {
                Amount = 4200,
                Ccy = 980,
                CardData = new CardData { Pan = "4242424242424242", Exp = "0642", Cvv = "123" },
                InitiationKind = InitiationKind.Merchant,
            }),
            """{"invoiceId":"p2","status":"success","amount":4200,"ccy":980,"tdsUrl":"https://3ds"}""");
        Assert.Equal("/api/merchant/invoice/payment-direct", direct.Path);
        Assert.Equal("4242424242424242", JsonNode.Parse(direct.Body)!["cardData"]!["pan"]!.GetValue<string>());
        Assert.Equal("https://3ds", ((PaymentResult)direct.Result!).TdsUrl);

        var sync = await Call(
            c => c.SyncPaymentAsync(new SyncPaymentRequest
            {
                Amount = 4200,
                Ccy = 980,
                ApplePay = new ApplePayPayload { Token = "t", Exp = "0642", EciIndicator = "5" },
            }),
            """{"invoiceId":"p2","status":"success","amount":4200,"ccy":980}""");
        Assert.Equal("/api/merchant/invoice/sync-payment", sync.Path);
        Assert.Equal("t", JsonNode.Parse(sync.Body)!["applePay"]!["token"]!.GetValue<string>());
        Assert.Null(JsonNode.Parse(sync.Body)!["cardData"]);
    }

    [Fact]
    public async Task Qr_merchant_statement_employees_submerchants_pubkey()
    {
        var list = await Call(c => c.GetQrListAsync(), """{"list":[{"qrId":"XJ","shortQrId":"OBJE","amountType":"merchant","pageUrl":"https://pay.mbnk.biz/XJ"}]}""");
        Assert.Equal("/api/merchant/qr/list", list.Path);
        Assert.Equal("XJ", ((IReadOnlyList<QrCashier>)list.Result!)[0].QrId);

        var details = await Call(c => c.GetQrDetailsAsync("XJ"), """{"shortQrId":"OBJE","amount":4200,"ccy":980,"invoiceId":"inv"}""");
        Assert.Equal("?qrId=XJ", details.Query);
        Assert.Equal(4200, ((QrDetails)details.Result!).Amount);

        var reset = await Call(c => c.ResetQrAmountAsync("XJ"), "");
        Assert.Equal(HttpMethod.Post, reset.Method);
        Assert.Equal("/api/merchant/qr/reset-amount", reset.Path);
        Assert.Equal("XJ", JsonNode.Parse(reset.Body)!["qrId"]!.GetValue<string>());

        var merchant = await Call(c => c.GetMerchantDetailsAsync(), """{"merchantId":"m","merchantName":"Co","edrpou":"4242424242"}""");
        Assert.Equal("/api/merchant/details", merchant.Path);
        Assert.Equal("4242424242", ((MerchantDetails)merchant.Result!).Edrpou);

        var statement = await Call(
            c => c.GetStatementAsync(1_649_329_978, 1_649_329_999, "code-1"),
            """{"list":[{"invoiceId":"inv","status":"success","amount":4200,"ccy":980,"profitAmount":4100,"date":"2019-08-24T14:15:22Z","cancelList":[{"amount":100,"ccy":980,"maskedPan":"4444"}]}]}""");
        Assert.Equal("/api/merchant/statement", statement.Path);
        Assert.Contains("from=1649329978", statement.Query);
        Assert.Contains("to=1649329999", statement.Query);
        Assert.Contains("code=code-1", statement.Query);
        var row = ((IReadOnlyList<StatementItem>)statement.Result!)[0];
        Assert.Equal(4100, row.ProfitAmount);
        Assert.Equal(100, row.CancelList![0].Amount);

        var employees = await Call(c => c.GetEmployeesAsync(), """{"list":[{"id":"e","name":"Ann","extRef":"x"}]}""");
        Assert.Equal("/api/merchant/employee/list", employees.Path);
        Assert.Equal("Ann", ((IReadOnlyList<Employee>)employees.Result!)[0].Name);

        var subs = await Call(c => c.GetSubmerchantsAsync(), """{"list":[{"code":"c","edrpou":"1","iban":"UA","owner":"ТОВ"}]}""");
        Assert.Equal("/api/merchant/submerchant/list", subs.Path);
        Assert.Equal("UA", ((IReadOnlyList<Submerchant>)subs.Result!)[0].Iban);

        var key = await Call(c => c.GetPubKeyAsync(), """{"key":"abc"}""");
        Assert.Equal("/api/merchant/pubkey", key.Path);
        Assert.Equal("abc", ((PubKeyResponse)key.Result!).Key);
    }

    [Fact]
    public async Task Wallet()
    {
        var cards = await Call(c => c.GetWalletAsync("wal"), """{"wallet":[{"cardToken":"ct","maskedPan":"4242","country":"804"}]}""");
        Assert.Equal("/api/merchant/wallet", cards.Path);
        Assert.Equal("?walletId=wal", cards.Query);
        Assert.Equal("ct", ((IReadOnlyList<WalletCard>)cards.Result!)[0].CardToken);

        var pay = await Call(
            c => c.WalletPaymentAsync(new WalletPaymentRequest
            {
                CardToken = "ct",
                Amount = 4200,
                Ccy = 980,
                InitiationKind = InitiationKind.Client,
            }),
            """{"invoiceId":"p2","status":"processing","amount":4200,"ccy":980}""");
        Assert.Equal("/api/merchant/wallet/payment", pay.Path);
        Assert.Equal("client", JsonNode.Parse(pay.Body)!["initiationKind"]!.GetValue<string>());

        var del = await Call(c => c.DeleteCardAsync("ct"), "");
        Assert.Equal(HttpMethod.Delete, del.Method);
        Assert.Equal("/api/merchant/wallet/card", del.Path);
        Assert.Equal("?cardToken=ct", del.Query);
    }

    [Fact]
    public async Task Subscriptions()
    {
        var from = new DateTimeOffset(2024, 6, 26, 15, 12, 44, TimeSpan.Zero);
        var created = await Call(
            c => c.CreateSubscriptionAsync(new CreateSubscriptionRequest
            {
                Amount = 4200,
                Interval = "1m",
                WebHookUrls = new SubscriptionWebHooks { ChargeUrl = "https://ex/charge", StatusUrl = "https://ex/status" },
            }),
            """{"subscriptionId":"s1","pageUrl":"https://pay.mbnk.biz/s1"}""");
        Assert.Equal("/api/merchant/subscription/create", created.Path);
        Assert.Equal("1m", JsonNode.Parse(created.Body)!["interval"]!.GetValue<string>());
        Assert.Equal("s1", ((CreateSubscriptionResponse)created.Result!).SubscriptionId);

        var status = await Call(
            c => c.GetSubscriptionStatusAsync("s1"),
            """{"subscriptionId":"s1","status":"active","amount":4200,"ccy":980,"interval":"1m","summary":{"totalPaid":2,"totalFailed":1},"walletData":{"cardToken":"ct","status":"created"}}""");
        Assert.Equal("?subscriptionId=s1", status.Query);
        Assert.Equal(2, ((SubscriptionStatus)status.Result!).Summary!.TotalPaid);

        var payments = await Call(
            c => c.GetSubscriptionPaymentsAsync("s1", from, limit: 20, page: 1),
            """{"payments":[{"amount":4200,"ccy":980,"status":"success","chargedAt":"2024-06-26T15:12:44Z"}],"pagination":{"totalItems":1,"itemsPerPage":20,"currentPage":1,"totalPages":1}}""");
        Assert.Equal("/api/merchant/subscription/payments", payments.Path);
        Assert.Contains("dateFrom=2024-06-26T15:12:44+00:00", Uri.UnescapeDataString(payments.Query));
        Assert.Contains("limit=20", payments.Query);
        Assert.Equal(1, ((SubscriptionPaymentsPage)payments.Result!).Pagination!.TotalItems);

        var list = await Call(
            c => c.GetSubscriptionsAsync(from, status: "active"),
            """{"list":[{"subscriptionId":"s1","amount":4200,"interval":"1m","status":"active"}],"pagination":{"totalItems":1,"itemsPerPage":20,"currentPage":1,"totalPages":1}}""");
        Assert.Equal("/api/merchant/subscription/list", list.Path);
        Assert.Contains("status=active", list.Query);

        var remove = await Call(c => c.RemoveSubscriptionAsync("s1"), "");
        Assert.Equal("/api/merchant/subscription/remove", remove.Path);
        Assert.Equal("s1", JsonNode.Parse(remove.Body)!["subscriptionId"]!.GetValue<string>());

        var edit = await Call(
            c => c.EditSubscriptionAsync(new EditSubscriptionRequest { SubscriptionId = "s1", Action = SubscriptionAction.Cancel, RefundAmount = 100 }),
            "");
        Assert.Equal("/api/merchant/subscription/edit", edit.Path);
        Assert.Equal(100, JsonNode.Parse(edit.Body)!["refundAmount"]!.GetValue<int>());
    }

    [Fact]
    public async Task Split_monopay_pos_t2p()
    {
        var receivers = await Call(c => c.GetSplitReceiversAsync(), """{"list":[{"splitReceiverId":"r","okpo":"1","name":"A"}]}""");
        Assert.Equal("/api/merchant/split-receiver/list", receivers.Path);
        Assert.Equal("r", ((IReadOnlyList<SplitReceiver>)receivers.Result!)[0].SplitReceiverId);

        var one = await Call(c => c.GetSplitReceiverAsync("r"), """{"splitReceiverId":"r","okpo":"1","name":"A"}""");
        Assert.Equal("/api/merchant/split-receiver/get-by-id", one.Path);
        Assert.Equal("?splitReceiverId=r", one.Query);

        var imported = await Call(
            c => c.ImportMonoPayKeyAsync(new ImportMonoPayKeyRequest { KeyValue = "pem", KeyName = "main" }),
            """{"result":{"keyId":"k1"}}""");
        Assert.Equal("/api/merchant/monopay/pubkey-import", imported.Path);
        Assert.Equal("k1", (string)imported.Result!);
        Assert.Equal("main", JsonNode.Parse(imported.Body)!["keyName"]!.GetValue<string>());

        var keys = await Call(c => c.GetMonoPayKeysAsync(), """{"result":[{"keyId":"k1","keyName":"main"}]}""");
        Assert.Equal("/api/merchant/monopay/pubkey-list", keys.Path);
        Assert.Equal("k1", ((IReadOnlyList<MonoPayKey>)keys.Result!)[0].KeyId);

        var del = await Call(c => c.DeleteMonoPayKeyAsync("k1"), "");
        Assert.Equal("/api/merchant/monopay/pubkey-delete", del.Path);
        Assert.Equal("k1", JsonNode.Parse(del.Body)!["keyId"]!.GetValue<string>());

        var pos = await Call(
            c => c.CancelPosTransactionAsync("123456789012", 4200),
            """{"cancelRef":"cr","status":"processing","rrn":"123456789012","amount":4200}""");
        Assert.Equal(HttpMethod.Post, pos.Method);
        Assert.Equal("/api/merchant/pos-transaction-cancel", pos.Path);
        Assert.Equal("uk", pos.Header("Accept-Language"));
        Assert.Equal(4200, JsonNode.Parse(pos.Body)!["amount"]!.GetValue<int>());
        Assert.Equal("cr", ((PosRefund)pos.Result!).CancelRef);

        var empty = await Call(c => c.CancelPosTransactionAsync("rrn", 1, "en"), "");
        Assert.Equal("en", empty.Header("Accept-Language"));
        Assert.Null(((PosRefund)empty.Result!).CancelRef);

        var posStatus = await Call(
            c => c.GetPosCancelStatusAsync("cr"),
            """{"cancelRef":"cr","status":"success","mystery":"kept"}""");
        Assert.Equal("/api/merchant/pos-transaction-cancel/status", posStatus.Path);
        Assert.Equal("?cancelRef=cr", posStatus.Query);
        var refund = (PosRefund)posStatus.Result!;
        Assert.Equal("success", refund.Status);
        Assert.Equal("kept", refund.Extra!["mystery"].GetString());

        var terminals = await Call(c => c.GetTerminalsAsync(), """{"list":[{"code":"c","name":"n","terminal":"MT1"}]}""");
        Assert.Equal("/api/merchant/t2p/terminal/list", terminals.Path);
        Assert.Equal("MT1", ((IReadOnlyList<Terminal>)terminals.Result!)[0].TerminalId);

        var t2p = await Call(
            c => c.GetT2pPaymentStatusAsync("ext-1"),
            """{"bank":"АТ Універсал Банк","terminal":"MT1","paymentType":"purchase","amount":4200,"ccy":"UAH","status":"rejected","externalPaymentId":"ext-1","dataTime":"2024-01-01T00:00:00Z","errorMessage":"no"}""");
        Assert.Equal("/api/merchant/t2p/terminal/payment/external/status", t2p.Path);
        Assert.Equal("?externalPaymentId=ext-1", t2p.Query);
        var payment = (T2pPaymentStatus)t2p.Result!;
        Assert.Equal("UAH", payment.Ccy);
        Assert.Equal("rejected", payment.Status);
        Assert.Equal("2024-01-01T00:00:00Z", payment.DataTime);
    }

    [Fact]
    public async Task Error_body_becomes_exception()
    {
        var ex = await Assert.ThrowsAsync<MonoAcquiringException>(() => Call(
            c => c.GetInvoiceStatusAsync("missing"),
            """{"errCode":"NOT_FOUND","errText":"invoice not found"}""",
            HttpStatusCode.NotFound));
        Assert.Equal(404, ex.StatusCode);
        Assert.Equal(ApiErrCode.NotFound, ex.ErrCode);
        Assert.Equal("invoice not found", ex.ErrText);
    }

    [Fact]
    public async Task Cms_headers_are_sent_when_set()
    {
        var cap = await Call(
            c => c.GetMerchantDetailsAsync(),
            """{"merchantId":"m"}""",
            cms: "opencart",
            cmsVersion: "3.0");
        Assert.Equal("opencart", cap.Header("X-Cms"));
        Assert.Equal("3.0", cap.Header("X-Cms-Version"));
    }

    [Fact]
    public void Webhook_accepts_official_sample_and_rejects_a_flipped_bit()
    {
        var body = OfficialWebhookBody();
        Assert.True(WebhookVerifier.Verify(body, OfficialXSign, OfficialPubKey));
        var parsed = InvoiceStatus.Parse(body);
        Assert.Equal("p2_9ZgpZVsl3", parsed.InvoiceId);
        Assert.Equal(InvoiceState.Created, parsed.Status);

        var bad = Convert.FromBase64String(OfficialXSign);
        bad[^1] ^= 0x01;
        Assert.False(WebhookVerifier.Verify(body, Convert.ToBase64String(bad), OfficialPubKey));
    }

    [Fact]
    public void MonoPay_signature_verifies_against_exported_public_key()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var order = """{"amount":4200,"ccy":980}""";
        const string requestId = "req-1";
        var signature = MonoPaySigner.Sign(order, requestId, key.ExportPkcs8PrivateKeyPem());
        var pub = Convert.ToBase64String(Encoding.UTF8.GetBytes(key.ExportSubjectPublicKeyInfoPem()));
        var payload = Encoding.UTF8.GetBytes(order + requestId);
        Assert.True(WebhookVerifier.Verify(payload, signature, pub));
        Assert.False(WebhookVerifier.Verify(Encoding.UTF8.GetBytes(order + "other"), signature, pub));
    }

    private static async Task<Captured> Call(
        Func<MonoAcquiringClient, Task> call,
        string responseBody,
        HttpStatusCode status = HttpStatusCode.OK,
        string? cms = null,
        string? cmsVersion = null)
    {
        object? result = null;
        var cap = await Send(responseBody, status, cms, cmsVersion, async client =>
        {
            await call(client);
        });
        return cap with { Result = result };

        async Task<Captured> Send(string body, HttpStatusCode code, string? cmsName, string? cmsVer, Func<MonoAcquiringClient, Task> action)
        {
            var handler = new CaptureHandler(_ => new HttpResponseMessage(code)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            });
            var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.monobank.ua/") };
            var client = new MonoAcquiringClient(http, new MonoAcquiringOptions
            {
                Token = Token,
                Cms = cmsName,
                CmsVersion = cmsVer,
            });
            await action(client);
            return handler.Captured;
        }
    }

    private static async Task<Captured> Call<T>(
        Func<MonoAcquiringClient, Task<T>> call,
        string responseBody,
        HttpStatusCode status = HttpStatusCode.OK,
        string? cms = null,
        string? cmsVersion = null)
    {
        var handler = new CaptureHandler(_ => new HttpResponseMessage(status)
        {
            Content = new StringContent(responseBody, Encoding.UTF8, "application/json"),
        });
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.monobank.ua/") };
        var client = new MonoAcquiringClient(http, new MonoAcquiringOptions
        {
            Token = Token,
            Cms = cms,
            CmsVersion = cmsVersion,
        });
        var result = await call(client);
        return handler.Captured with { Result = result };
    }

    private const string OfficialPubKey =
        "LS0tLS1CRUdJTiBQVUJMSUMgS0VZLS0tLS0KTUZrd0V3WUhLb1pJemowQ0FRWUlLb1pJemowREFRY0RRZ0FFQUc1LzZ3NnZubGJZb0ZmRHlYWE4vS29CbVVjTgo3NWJSUWg4MFBhaEdldnJoanFCQnI3OXNSS0JSbnpHODFUZVQ5OEFOakU1c0R3RmZ5Znhub0ZJcmZBPT0KLS0tLS1FTkQgUFVCTElDIEtFWS0tLS0tCg==";

    private const string OfficialXSign =
        "MEUCIQC/mVKhi8FKoayul2Mim3E2oaIOCNJk5dEXxTqbkeJSOQIgOM0hsW0qcP2H8iXy1aQYpmY0SJWEaWur7nQXlKDCFxA=";

    // Docs paste the Go sample with a ragged indent. The published X-Sign matches the same JSON at a flat 2-space indent.
    private static byte[] OfficialWebhookBody() =>
        """
        {
          "invoiceId": "p2_9ZgpZVsl3",
          "status": "created",
          "failureReason": "string",
          "amount": 4200,
          "ccy": 980,
          "finalAmount": 4200,
          "createdDate": "2019-08-24T14:15:22Z",
          "modifiedDate": "2019-08-24T14:15:22Z",
          "reference": "84d0070ee4e44667b31371d8f8813947",
          "cancelList": [
            {
              "status": "processing",
              "amount": 4200,
              "ccy": 980,
              "createdDate": "2019-08-24T14:15:22Z",
              "modifiedDate": "2019-08-24T14:15:22Z",
              "approvalCode": "662476",
              "rrn": "060189181768",
              "extRef": "635ace02599849e981b2cd7a65f417fe"
            }
          ]
        }
        """u8.ToArray();

    private sealed class CaptureHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public Captured Captured { get; private set; } = null!;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken);
            var headers = request.Headers.ToDictionary(h => h.Key, h => string.Join(",", h.Value), StringComparer.OrdinalIgnoreCase);
            Captured = new Captured(request.Method, request.RequestUri!.AbsolutePath, request.RequestUri.Query, body, headers, null);
            return respond(request);
        }
    }

    private sealed record Captured(
        HttpMethod Method,
        string Path,
        string Query,
        string Body,
        IReadOnlyDictionary<string, string> Headers,
        object? Result)
    {
        public string? Header(string name) => Headers.TryGetValue(name, out var value) ? value : null;
    }
}
