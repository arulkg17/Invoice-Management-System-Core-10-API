using Invoice.BAL.Contracts;
using Invoice.Model;

namespace Invoice.CqrsAPI.Features.Vendors;

public static class VendorChecks
{
    /// <summary>Business rule shared by Create and Update: the vendor code must be unique.</summary>
    public static async Task EnsureCodeIsUniqueAsync(IVendorService service, string code, int? excludeId)
    {
        var all = await service.GetAllAsync();

        var duplicate = all.Any(x =>
            string.Equals(x.VendorCode?.Trim(), code, StringComparison.OrdinalIgnoreCase) &&
            x.Id != excludeId);

        if (duplicate)
        {
            throw new BusinessRuleException($"Vendor code '{code}' already exists.");
        }
    }
}
