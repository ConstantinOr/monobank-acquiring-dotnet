namespace MonoBank.Acquiring;

public static class PaymentType
{
    public const string Debit = "debit";
    public const string Hold = "hold";
    public const string Verification = "verification";
}

public static class InvoiceState
{
    public const string Created = "created";
    public const string Processing = "processing";
    public const string Hold = "hold";
    public const string Success = "success";
    public const string Failure = "failure";
    public const string Reversed = "reversed";
    public const string Expired = "expired";
}

public static class InitiationKind
{
    public const string Merchant = "merchant";
    public const string Client = "client";
}

public static class DisplayType
{
    public const string Iframe = "iframe";
}

public static class DiscountType
{
    public const string Discount = "DISCOUNT";
    public const string ExtraCharge = "EXTRA_CHARGE";
}

public static class DiscountMode
{
    public const string Percent = "PERCENT";
    public const string Value = "VALUE";
}

public static class SubscriptionAction
{
    public const string Cancel = "cancel";
}

public static class ApiErrCode
{
    public const string BadRequest = "BAD_REQUEST";
    public const string Forbidden = "FORBIDDEN";
    public const string NotFound = "NOT_FOUND";
    public const string MethodNotAllowed = "METHOD_NOT_ALLOWED";
    public const string TooManyRequests = "TMR";
    public const string InternalError = "INTERNAL_ERROR";
}
