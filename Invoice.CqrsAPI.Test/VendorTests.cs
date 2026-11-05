using Invoice.BAL.Contracts;
using Invoice.CqrsAPI.Features.Vendors;
using Invoice.CqrsAPI.Features.Vendors.Commands;
using Invoice.DTOs;
using Invoice.Model;
using Moq;

namespace Invoice.CqrsAPI.Test;

public class VendorTests
{
    private readonly Mock<IVendorService> _service = new();
    private readonly Mock<IPurchaseOrderService> _orders = new();

    private static VendorData Data(string code = "C01", string name = "Acme", string? email = null, string? mobile = null)
        => new(code, name, null, mobile, email, null, null, null, null, null, null, null, true);

    [Fact]
    public void Valid_command_has_no_failures()
    {
        var failures = new CreateVendorValidator().Validate(new CreateVendorCommand(Data(email: "a@b.com", mobile: "+91 98765 43210"), "admin"));

        Assert.Empty(failures);
    }

    [Theory]
    [InlineData("", "Acme", "VendorCode")]
    [InlineData("C01", " ", "VendorName")]
    public void Blank_code_or_name_fails(string code, string name, string property)
    {
        var failures = new CreateVendorValidator().Validate(new CreateVendorCommand(Data(code, name), "admin"));

        Assert.Contains(failures, f => f.Property == property);
    }

    [Fact]
    public void Bad_email_and_bad_mobile_fail()
    {
        var failures = new CreateVendorValidator().Validate(new CreateVendorCommand(Data(email: "not-an-email", mobile: "abc"), "admin"));

        Assert.Contains(failures, f => f.Property == "Email");
        Assert.Contains(failures, f => f.Property == "MobileNo");
    }

    [Fact]
    public async Task Create_with_duplicate_code_throws_and_does_not_save()
    {
        _service.Setup(s => s.GetAllAsync()).ReturnsAsync(new List<VendorDto>
        {
            new() { Id = 1, VendorCode = "c01", VendorName = "Existing" }
        }.AsEnumerable());

        var handler = new CreateVendorHandler(_service.Object);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            handler.HandleAsync(new CreateVendorCommand(Data(), "admin"), CancellationToken.None));

        _service.Verify(s => s.AddAsync(It.IsAny<VendorDto>()), Times.Never);
    }

    [Fact]
    public async Task Create_saves_and_returns_id()
    {
        _service.Setup(s => s.GetAllAsync()).ReturnsAsync(Array.Empty<VendorDto>().AsEnumerable());
        _service.Setup(s => s.AddAsync(It.IsAny<VendorDto>())).ReturnsAsync(11);

        var id = await new CreateVendorHandler(_service.Object)
            .HandleAsync(new CreateVendorCommand(Data(), "admin"), CancellationToken.None);

        Assert.Equal(11, id);
        _service.Verify(s => s.AddAsync(It.Is<VendorDto>(d => d.VendorCode == "C01" && d.CreatedBy == "admin")), Times.Once);
    }

    [Fact]
    public async Task Update_unknown_id_throws_not_found()
    {
        _service.Setup(s => s.GetByIdAsync(5)).ReturnsAsync((VendorDto?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            new UpdateVendorHandler(_service.Object)
                .HandleAsync(new UpdateVendorCommand(5, Data(), "editor"), CancellationToken.None));
    }

    [Fact]
    public async Task Update_keeps_created_audit_values()
    {
        var existing = new VendorDto { Id = 5, VendorCode = "OLD", VendorName = "Old", IsActive = true, CreatedBy = "creator" };

        _service.Setup(s => s.GetByIdAsync(5)).ReturnsAsync(existing);
        _service.Setup(s => s.GetAllAsync()).ReturnsAsync(new List<VendorDto> { existing }.AsEnumerable());
        _service.Setup(s => s.UpdateAsync(It.IsAny<VendorDto>())).ReturnsAsync(true);

        await new UpdateVendorHandler(_service.Object)
            .HandleAsync(new UpdateVendorCommand(5, Data("NEW", "New"), "editor"), CancellationToken.None);

        _service.Verify(s => s.UpdateAsync(It.Is<VendorDto>(d =>
            d.VendorCode == "NEW" && d.UpdatedBy == "editor" && d.CreatedBy == "creator")), Times.Once);
    }

    [Fact]
    public async Task Delete_with_orderss_throws_business_rule()
    {
        _service.Setup(s => s.GetByIdAsync(5)).ReturnsAsync(new VendorDto { Id = 5, VendorCode = "C", VendorName = "N" });
        _orders.Setup(s => s.GetAllPagedAsync(null, 5, null, 1, 1))
            .ReturnsAsync(new PagedResultDto<PurchaseOrderDto> { TotalRecords = 2 });

        var handler = new DeleteVendorHandler(_service.Object, _orders.Object);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            handler.HandleAsync(new DeleteVendorCommand(5), CancellationToken.None));

        _service.Verify(s => s.DeleteAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Delete_without_orderss_calls_service()
    {
        _service.Setup(s => s.GetByIdAsync(5)).ReturnsAsync(new VendorDto { Id = 5, VendorCode = "C", VendorName = "N" });
        _orders.Setup(s => s.GetAllPagedAsync(null, 5, null, 1, 1))
            .ReturnsAsync(new PagedResultDto<PurchaseOrderDto> { TotalRecords = 0 });
        _service.Setup(s => s.DeleteAsync(5)).ReturnsAsync(true);

        await new DeleteVendorHandler(_service.Object, _orders.Object)
            .HandleAsync(new DeleteVendorCommand(5), CancellationToken.None);

        _service.Verify(s => s.DeleteAsync(5), Times.Once);
    }
}
