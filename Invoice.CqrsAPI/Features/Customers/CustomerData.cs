using Invoice.CqrsAPI.Common;
using Invoice.CqrsAPI.Cqrs;
using Invoice.DTOs;

namespace Invoice.CqrsAPI.Features.Customers;

/// <summary>The editable fields of a customer. Create and Update both carry one of these.</summary>
public sealed record CustomerData(
    string CustomerCode,
    string CustomerName,
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
    public static CustomerData From(CustomerDto dto) => new(
        dto.CustomerCode, dto.CustomerName, dto.ContactPerson, dto.MobileNo, dto.Email,
        dto.Address1, dto.Address2, dto.City, dto.State, dto.Country,
        dto.ZipCode, dto.GstNo, dto.IsActive);
}

/// <summary>Implemented by every command that carries a CustomerData, so one validator serves them all.</summary>
public interface IHasCustomerData
{
    CustomerData Data { get; }
}

/// <summary>Limits used by the validators. Match them to the Customer table definition.</summary>
public static class CustomerRules
{
    public const int CodeMaxLength = 50;
    public const int NameMaxLength = 200;
    public const int TextMaxLength = 200;
    public const int GstMaxLength = 15;
    public const int MaxPageSize = 500;
}

public abstract class CustomerDataValidator<T> : RuleValidator<T> where T : IHasCustomerData
{
    protected CustomerDataValidator()
    {
        Rule(c => !string.IsNullOrWhiteSpace(c.Data?.CustomerCode), "CustomerCode", "Customer code is required.");
        Rule(c => FieldChecks.MaxLength(c.Data?.CustomerCode, CustomerRules.CodeMaxLength), "CustomerCode",
            $"Customer code cannot be longer than {CustomerRules.CodeMaxLength} characters.");

        Rule(c => !string.IsNullOrWhiteSpace(c.Data?.CustomerName), "CustomerName", "Customer name is required.");
        Rule(c => FieldChecks.MaxLength(c.Data?.CustomerName, CustomerRules.NameMaxLength), "CustomerName",
            $"Customer name cannot be longer than {CustomerRules.NameMaxLength} characters.");

        Rule(c => FieldChecks.IsEmailOrEmpty(c.Data?.Email), "Email", "Email is not a valid address.");
        Rule(c => FieldChecks.IsPhoneOrEmpty(c.Data?.MobileNo), "MobileNo", "Mobile number is not valid.");
        Rule(c => FieldChecks.MaxLength(c.Data?.GstNo, CustomerRules.GstMaxLength), "GstNo",
            $"GST number cannot be longer than {CustomerRules.GstMaxLength} characters.");

        Rule(c => FieldChecks.MaxLength(c.Data?.ContactPerson, CustomerRules.TextMaxLength), "ContactPerson", "Contact person is too long.");
        Rule(c => FieldChecks.MaxLength(c.Data?.Address1, CustomerRules.TextMaxLength), "Address1", "Address line 1 is too long.");
        Rule(c => FieldChecks.MaxLength(c.Data?.Address2, CustomerRules.TextMaxLength), "Address2", "Address line 2 is too long.");
        Rule(c => FieldChecks.MaxLength(c.Data?.City, CustomerRules.TextMaxLength), "City", "City is too long.");
        Rule(c => FieldChecks.MaxLength(c.Data?.State, CustomerRules.TextMaxLength), "State", "State is too long.");
        Rule(c => FieldChecks.MaxLength(c.Data?.Country, CustomerRules.TextMaxLength), "Country", "Country is too long.");
        Rule(c => FieldChecks.MaxLength(c.Data?.ZipCode, 20), "ZipCode", "Zip code is too long.");
    }
}
