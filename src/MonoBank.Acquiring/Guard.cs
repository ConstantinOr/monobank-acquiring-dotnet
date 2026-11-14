using System.Runtime.CompilerServices;

namespace MonoBank.Acquiring;

internal static class Guard
{
    public static void NotNull(
        object? value,
        [CallerArgumentExpression(nameof(value))] string? name = null)
    {
        if (value is null)
            throw new ArgumentNullException(name);
    }

    public static void NotNullOrWhiteSpace(
        string? value,
        [CallerArgumentExpression(nameof(value))] string? name = null)
    {
        if (value is null)
            throw new ArgumentNullException(name);
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("The value cannot be an empty string or composed entirely of whitespace.", name);
    }
}
