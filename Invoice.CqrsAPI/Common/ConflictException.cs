namespace Invoice.CqrsAPI.Common;

/// <summary>The request clashes with existing data (duplicate user name / email). The middleware maps this to HTTP 409.</summary>
public sealed class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}
