namespace MonoBank.Acquiring;

public sealed class MonoAcquiringException : Exception
{
    public MonoAcquiringException(int statusCode, string? errCode, string? errText, string? body, string message)
        : base(message)
    {
        StatusCode = statusCode;
        ErrCode = errCode;
        ErrText = errText;
        Body = body;
    }

    public int StatusCode { get; }

    public string? ErrCode { get; }

    public string? ErrText { get; }

    public string? Body { get; }
}
