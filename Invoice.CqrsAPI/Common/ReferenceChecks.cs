using Invoice.BAL.Contracts;
using Invoice.Model;

namespace Invoice.CqrsAPI.Common;

/// <summary>
/// "Does the thing I point to exist and is it usable?" - asked by the Purchase Order and Sales Invoice
/// handlers. One shared place, so the rules and the messages are identical everywhere.
/// </summary>
public interface IReferenceChecks
{
    Task EnsureVendorAsync(int vendorId);
    Task EnsureCustomerAsync(int customerId);
    Task EnsureItemsAsync(IEnumerable<int> itemmasterIds);
}

public sealed class ReferenceChecks : IReferenceChecks
{
    private readonly IVendorService _vendors;
    private readonly ICustomerService _customers;
    private readonly IItemmasterService _items;

    public ReferenceChecks(IVendorService vendors, ICustomerService customers, IItemmasterService items)
    {
        _vendors = vendors;
        _customers = customers;
        _items = items;
    }

    public async Task EnsureVendorAsync(int vendorId)
    {
        var vendor = await _vendors.GetByIdAsync(vendorId)
            ?? throw new NotFoundException($"Vendor with Id {vendorId} was not found.");

        if (vendor.IsActive == false)
            throw new BusinessRuleException($"Vendor '{vendor.VendorName}' is inactive.");
    }

    public async Task EnsureCustomerAsync(int customerId)
    {
        var customer = await _customers.GetByIdAsync(customerId)
            ?? throw new NotFoundException($"Customer with Id {customerId} was not found.");

        if (customer.IsActive == false)
            throw new BusinessRuleException($"Customer '{customer.CustomerName}' is inactive.");
    }

    public async Task EnsureItemsAsync(IEnumerable<int> itemmasterIds)
    {
        foreach (var id in itemmasterIds.Distinct())
        {
            var item = await _items.GetByIdAsync(id)
                ?? throw new NotFoundException($"Item with Id {id} was not found.");

            if (item.IsActive == false)
                throw new BusinessRuleException($"Item '{item.ItemName}' is inactive.");
        }
    }
}
