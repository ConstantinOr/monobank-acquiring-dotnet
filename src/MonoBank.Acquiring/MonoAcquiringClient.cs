using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace MonoBank.Acquiring;

public sealed partial class MonoAcquiringClient
{
    private readonly HttpClient _http;
    private readonly MonoAcquiringOptions _options;

    public MonoAcquiringClient(HttpClient http, MonoAcquiringOptions options)
    {
        ArgumentNullException.ThrowIfNull(http);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.Token);
        _http = http;
        _options = options;
    }

    private Uri BaseAddress
    {
        get
        {
            var uri = _http.BaseAddress ?? _options.BaseAddress;
            return uri.AbsoluteUri.EndsWith('/') ? uri : new Uri(uri.AbsoluteUri + "/");
        }
    }

    private async Task SendAsync(
        HttpMethod method,
        string path,
        object? body,
        CancellationToken cancellationToken,
        IEnumerable<KeyValuePair<string, string?>>? query = null,
        string? acceptLanguage = null)
    {
        using var response = await SendRawAsync(method, path, body, cancellationToken, query, acceptLanguage).ConfigureAwait(false);
        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
    }

    private async Task<T> SendAsync<T>(
        HttpMethod method,
        string path,
        object? body,
        CancellationToken cancellationToken,
        IEnumerable<KeyValuePair<string, string?>>? query = null,
        string? acceptLanguage = null)
    {
        using var response = await SendRawAsync(method, path, body, cancellationToken, query, acceptLanguage).ConfigureAwait(false);
        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
        var text = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new MonoAcquiringException((int)response.StatusCode, null, null, text, "Monobank acquiring API returned an empty body.");
        }

        var value = JsonSerializer.Deserialize<T>(text, AcquiringJson.Options);
        if (value is null)
        {
            throw new MonoAcquiringException((int)response.StatusCode, null, null, text, "Monobank acquiring API returned an empty JSON value.");
        }

        return value;
    }

    private async Task<T> SendAllowEmptyAsync<T>(
        HttpMethod method,
        string path,
        object? body,
        CancellationToken cancellationToken,
        string? acceptLanguage = null)
        where T : new()
    {
        using var response = await SendRawAsync(method, path, body, cancellationToken, acceptLanguage: acceptLanguage).ConfigureAwait(false);
        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
        var text = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(text))
            return new T();

        return JsonSerializer.Deserialize<T>(text, AcquiringJson.Options) ?? new T();
    }

    private async Task<HttpResponseMessage> SendRawAsync(
        HttpMethod method,
        string path,
        object? body,
        CancellationToken cancellationToken,
        IEnumerable<KeyValuePair<string, string?>>? query = null,
        string? acceptLanguage = null)
    {
        using var request = new HttpRequestMessage(method, Build(path, query));
        request.Headers.TryAddWithoutValidation("X-Token", _options.Token);
        if (!string.IsNullOrEmpty(_options.Cms))
            request.Headers.TryAddWithoutValidation("X-Cms", _options.Cms);
        if (!string.IsNullOrEmpty(_options.CmsVersion))
            request.Headers.TryAddWithoutValidation("X-Cms-Version", _options.CmsVersion);
        if (!string.IsNullOrEmpty(acceptLanguage))
            request.Headers.TryAddWithoutValidation("Accept-Language", acceptLanguage);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (body is not null)
            request.Content = JsonContent.Create(body, options: AcquiringJson.Options);

        return await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }

    private Uri Build(string path, IEnumerable<KeyValuePair<string, string?>>? query)
    {
        var uri = new Uri(BaseAddress, path);
        if (query is null)
            return uri;

        var parts = new List<string>();
        foreach (var (name, value) in query)
        {
            if (string.IsNullOrEmpty(value))
                continue;
            parts.Add($"{Uri.EscapeDataString(name)}={Uri.EscapeDataString(value)}");
        }

        if (parts.Count == 0)
            return uri;

        return new Uri(uri.AbsoluteUri + "?" + string.Join("&", parts));
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
            return;

        var body = response.Content is null
            ? ""
            : await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        string? code = null;
        string? text = null;
        if (!string.IsNullOrWhiteSpace(body))
        {
            try
            {
                var err = JsonSerializer.Deserialize<ApiErrorBody>(body, AcquiringJson.Options);
                code = err?.ErrCode;
                text = err?.ErrText;
            }
            catch (JsonException)
            {
            }
        }

        var message = text ?? (string.IsNullOrWhiteSpace(body) ? response.ReasonPhrase : body);
        throw new MonoAcquiringException(
            (int)response.StatusCode,
            code,
            text,
            body,
            $"Monobank acquiring API returned {(int)response.StatusCode}: {message}");
    }

    private static KeyValuePair<string, string?> Q(string name, string? value) => new(name, value);

    private static KeyValuePair<string, string?> Q(string name, int? value) =>
        new(name, value?.ToString(CultureInfo.InvariantCulture));

    private static KeyValuePair<string, string?> Q(string name, long? value) =>
        new(name, value?.ToString(CultureInfo.InvariantCulture));

    private static KeyValuePair<string, string?> Q(string name, DateTimeOffset? value) =>
        new(name, AcquiringJson.Rfc3339(value));

    private static void RequireId(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{name} is required.", name);
    }

    private sealed class ApiErrorBody
    {
        public string? ErrCode { get; set; }

        public string? ErrText { get; set; }
    }
}
