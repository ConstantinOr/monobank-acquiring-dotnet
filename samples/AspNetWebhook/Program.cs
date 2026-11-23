using System.Text;
using MonoBank.Acquiring;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMonoAcquiring(builder.Configuration);

var app = builder.Build();

app.MapGet("/", () => Results.Content(
    """
    MonoBank acquiring sample.
    POST /invoices — create a 42.00 UAH invoice (returns pageUrl).
    POST /mono/webhook — monobank status callback (ECDSA verified).
    """,
    "text/plain",
    Encoding.UTF8));

app.MapPost("/invoices", async (MonoAcquiringClient mono, HttpRequest request, CancellationToken ct) =>
{
    var baseUrl = $"{request.Scheme}://{request.Host}";
    var invoice = await mono.CreateInvoiceAsync(new CreateInvoiceRequest
    {
        Amount = 4200,
        Ccy = 980,
        RedirectUrl = $"{baseUrl}/paid",
        WebHookUrl = $"{baseUrl}/mono/webhook",
        MerchantPaymInfo = new MerchantPaymInfo
        {
            Reference = $"aspnet-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}",
            Destination = "AspNetWebhook sample",
        },
    }, ct);

    return Results.Ok(new
    {
        invoice.InvoiceId,
        invoice.PageUrl,
        tip = "Open pageUrl to pay on monobank hosted checkout.",
    });
});

app.MapGet("/paid", () => Results.Text("Payment return — check webhook logs / invoice status."));

app.MapPost("/mono/webhook", async (MonoAcquiringClient mono, HttpRequest request, CancellationToken ct) =>
{
    using var ms = new MemoryStream();
    await request.Body.CopyToAsync(ms, ct);
    var body = ms.ToArray();

    if (!request.Headers.TryGetValue("X-Sign", out var xSign) || string.IsNullOrWhiteSpace(xSign))
        return Results.Unauthorized();

    var pub = await mono.GetPubKeyAsync(ct);
    if (string.IsNullOrWhiteSpace(pub.Key) || !WebhookVerifier.Verify(body, xSign.ToString(), pub.Key))
        return Results.Unauthorized();

    var status = InvoiceStatus.Parse(body);
    app.Logger.LogInformation(
        "Webhook invoice={InvoiceId} status={Status} amount={Amount}",
        status.InvoiceId,
        status.Status,
        status.Amount);

    return Results.Ok();
});

app.Run();
