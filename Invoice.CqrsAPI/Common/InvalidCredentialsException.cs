namespace Invoice.CqrsAPI.Common;

/// <summary>Wrong user name or password. The middleware maps this to HTTP 401.</summary>
public sealed class InvalidCredentialsException : Exception
{
    public InvalidCredentialsException(string message) : base(message) { }
}
