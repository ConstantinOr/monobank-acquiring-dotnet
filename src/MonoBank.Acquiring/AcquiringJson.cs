using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MonoBank.Acquiring;

internal static class AcquiringJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
    };

    public static string? Rfc3339(DateTimeOffset? value) =>
        value?.ToString("yyyy-MM-dd'T'HH:mm:ssK", CultureInfo.InvariantCulture);
}

/// <summary>T2P <c>ccy</c> is alpha-3 in the samples and numeric everywhere else.</summary>
internal sealed class JsonStringOrNumberConverter : JsonConverter<string>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType switch
        {
            JsonTokenType.String => reader.GetString(),
            JsonTokenType.Number => reader.TryGetInt64(out var n)
                ? n.ToString(CultureInfo.InvariantCulture)
                : reader.GetDecimal().ToString(CultureInfo.InvariantCulture),
            JsonTokenType.Null => null,
            _ => throw new JsonException(),
        };

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value);
}
