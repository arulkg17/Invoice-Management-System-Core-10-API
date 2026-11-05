using Invoice.CqrsAPI.Common;
using Invoice.CqrsAPI.Cqrs;
using Invoice.DTOs;

namespace Invoice.CqrsAPI.Features.Vendors;

/// <summary>The editable fields of a vendor. Create and Update both carry one of these.</summary>
public sealed record VendorData(
    string VendorCode,
    string VendorName,
    string? ContactPerson,
    string? MobileNo,
    string? Email,
    string? Address1,
    string? Address2,
    string? City,
    string? State,
    string? Country,
    string? ZipCode,
    string? GstNo,
    bool? IsActive)
{
    public static VendorData From(VendorDto dto) => new(
        dto.VendorCode, dto.VendorName, dto.ContactPerson, dto.MobileNo, dto.Email,
        dto.Address1, dto.Address2, dto.City, dto.State, dto.Country,
        dto.ZipCode, dto.GstNo, dto.IsActive);
}

/// <summary>Implemented by every command that carries a VendorData, so one validator serves them all.</summary>
public interface IHasVendorData
{
    VendorData Data { get; }
}

/// <summary>Limits used by the validators. Match them to the Vendor table definition.</summary>
public static class VendorRules
{
    public const int CodeMaxLength = 50;
    public const int NameMaxLength = 200;
    public const int TextMaxLength = 200;
    public const int GstMaxLength = 15;
    public const int MaxPageSize = 500;
}

public abstract class VendorDataValidator<T> : RuleValidator<T> where T : IHasVendorData
{
    protected VendorDataValidator()
    {
        Rule(c => !string.IsNullOrWhiteSpace(c.Data?.VendorCode), "VendorCode", "Vendor code is required.");
        Rule(c => FieldChecks.MaxLength(c.Data?.VendorCode, VendorRules.CodeMaxLength), "VendorCode",
            $"Vendor code cannot be longer than {VendorRules.CodeMaxLength} characters.");

        Rule(c => !string.IsNullOrWhiteSpace(c.Data?.VendorName), "VendorName", "Vendor name is required.");
        Rule(c => FieldChecks.MaxLength(c.Data?.VendorName, VendorRules.NameMaxLength), "VendorName",
            $"Vendor name cannot be longer than {VendorRules.NameMaxLength} characters.");

        Rule(c => FieldChecks.IsEmailOrEmpty(c.Data?.Email), "Email", "Email is not a valid address.");
        Rule(c => FieldChecks.IsPhoneOrEmpty(c.Data?.MobileNo), "MobileNo", "Mobile number is not valid.");
        Rule(c => FieldChecks.MaxLength(c.Data?.GstNo, VendorRules.GstMaxLength), "GstNo",
            $"GST number cannot be longer than {VendorRules.GstMaxLength} characters.");

        Rule(c => FieldChecks.MaxLength(c.Data?.ContactPerson, VendorRules.TextMaxLength), "ContactPerson", "Contact person is too long.");
        Rule(c => FieldChecks.MaxLength(c.Data?.Address1, VendorRules.TextMaxLength), "Address1", "Address line 1 is too long.");
        Rule(c => FieldChecks.MaxLength(c.Data?.Address2, VendorRules.TextMaxLength), "Address2", "Address line 2 is too long.");
        Rule(c => FieldChecks.MaxLength(c.Data?.City, VendorRules.TextMaxLength), "City", "City is too long.");
        Rule(c => FieldChecks.MaxLength(c.Data?.State, VendorRules.TextMaxLength), "State", "State is too long.");
        Rule(c => FieldChecks.MaxLength(c.Data?.Country, VendorRules.TextMaxLength), "Country", "Country is too long.");
        Rule(c => FieldChecks.MaxLength(c.Data?.ZipCode, 20), "ZipCode", "Zip code is too long.");
    }
}
