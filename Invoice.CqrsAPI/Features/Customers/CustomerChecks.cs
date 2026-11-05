using Invoice.BAL.Contracts;
using Invoice.Model;

namespace Invoice.CqrsAPI.Features.Customers;

public static class CustomerChecks
{
    /// <summary>Business rule shared by Create and Update: the customer code must be unique.</summary>
    public static async Task EnsureCodeIsUniqueAsync(ICustomerService service, string code, int? excludeId)
    {
        var all = await service.GetAllAsync();

        var duplicate = all.Any(x =>
            string.Equals(x.CustomerCode?.Trim(), code, StringComparison.OrdinalIgnoreCase) &&
            x.Id != excludeId);

        if (duplicate)
        {
            throw new BusinessRuleException($"Customer code '{code}' already exists.");
        }
    }
}
