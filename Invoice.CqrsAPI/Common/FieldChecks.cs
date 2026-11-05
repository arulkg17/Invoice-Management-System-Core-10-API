using System.Net.Mail;

namespace Invoice.CqrsAPI.Common;

/// <summary>Small reusable checks for the validators. Optional fields: empty / null counts as valid.</summary>
public static class FieldChecks
{
    public static bool MaxLength(string? value, int max) => (value?.Trim().Length ?? 0) <= max;

    public static bool IsEmailOrEmpty(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return true;

        return MailAddress.TryCreate(value.Trim(), out var address) && address.Address == value.Trim();
    }

    /// <summary>Digits with an optional leading +, spaces, dashes and brackets; 7 to 15 digits.</summary>
    public static bool IsPhoneOrEmpty(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return true;

        var text = value.Trim();
        var digits = text.Count(char.IsDigit);

        return digits is >= 7 and <= 15
            && text.All(c => char.IsDigit(c) || c is '+' or ' ' or '-' or '(' or ')')
            && text.LastIndexOf('+') <= 0;
    }
}
