using Invoice.BAL.Contracts;
using Invoice.CqrsAPI.Features.Customers;
using Invoice.CqrsAPI.Features.Customers.Commands;
using Invoice.DTOs;
using Invoice.Model;
using Moq;

namespace Invoice.CqrsAPI.Test;

public class CustomerTests
{
    private readonly Mock<ICustomerService> _service = new();
    private readonly Mock<ISalesInvoiceService> _invoices = new();

    private static CustomerData Data(string code = "C01", string name = "Acme", string? email = null, string? mobile = null)
        => new(code, name, null, mobile, email, null, null, null, null, null, null, null, true);

    [Fact]
    public void Valid_command_has_no_failures()
    {
        var failures = new CreateCustomerValidator().Validate(new CreateCustomerCommand(Data(email: "a@b.com", mobile: "+91 98765 43210"), "admin"));

        Assert.Empty(failures);
    }

    [Theory]
    [InlineData("", "Acme", "CustomerCode")]
    [InlineData("C01", " ", "CustomerName")]
    public void Blank_code_or_name_fails(string code, string name, string property)
    {
        var failures = new CreateCustomerValidator().Validate(new CreateCustomerCommand(Data(code, name), "admin"));

        Assert.Contains(failures, f => f.Property == property);
    }

    [Fact]
    public void Bad_email_and_bad_mobile_fail()
    {
        var failures = new CreateCustomerValidator().Validate(new CreateCustomerCommand(Data(email: "not-an-email", mobile: "abc"), "admin"));

        Assert.Contains(failures, f => f.Property == "Email");
        Assert.Contains(failures, f => f.Property == "MobileNo");
    }

    [Fact]
    public async Task Create_with_duplicate_code_throws_and_does_not_save()
    {
        _service.Setup(s => s.GetAllAsync()).ReturnsAsync(new List<CustomerDto>
        {
            new() { Id = 1, CustomerCode = "c01", CustomerName = "Existing" }
        }.AsEnumerable());

        var handler = new CreateCustomerHandler(_service.Object);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            handler.HandleAsync(new CreateCustomerCommand(Data(), "admin"), CancellationToken.None));

        _service.Verify(s => s.AddAsync(It.IsAny<CustomerDto>()), Times.Never);
    }

    [Fact]
    public async Task Create_saves_and_returns_id()
    {
        _service.Setup(s => s.GetAllAsync()).ReturnsAsync(Array.Empty<CustomerDto>().AsEnumerable());
        _service.Setup(s => s.AddAsync(It.IsAny<CustomerDto>())).ReturnsAsync(11);

        var id = await new CreateCustomerHandler(_service.Object)
            .HandleAsync(new CreateCustomerCommand(Data(), "admin"), CancellationToken.None);

        Assert.Equal(11, id);
        _service.Verify(s => s.AddAsync(It.Is<CustomerDto>(d => d.CustomerCode == "C01" && d.CreatedBy == "admin")), Times.Once);
    }

    [Fact]
    public async Task Update_unknown_id_throws_not_found()
    {
        _service.Setup(s => s.GetByIdAsync(5)).ReturnsAsync((CustomerDto?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            new UpdateCustomerHandler(_service.Object)
                .HandleAsync(new UpdateCustomerCommand(5, Data(), "editor"), CancellationToken.None));
    }

    [Fact]
    public async Task Update_keeps_created_audit_values()
    {
        var existing = new CustomerDto { Id = 5, CustomerCode = "OLD", CustomerName = "Old", IsActive = true, CreatedBy = "creator" };

        _service.Setup(s => s.GetByIdAsync(5)).ReturnsAsync(existing);
        _service.Setup(s => s.GetAllAsync()).ReturnsAsync(new List<CustomerDto> { existing }.AsEnumerable());
        _service.Setup(s => s.UpdateAsync(It.IsAny<CustomerDto>())).ReturnsAsync(true);

        await new UpdateCustomerHandler(_service.Object)
            .HandleAsync(new UpdateCustomerCommand(5, Data("NEW", "New"), "editor"), CancellationToken.None);

        _service.Verify(s => s.UpdateAsync(It.Is<CustomerDto>(d =>
            d.CustomerCode == "NEW" && d.UpdatedBy == "editor" && d.CreatedBy == "creator")), Times.Once);
    }

    [Fact]
    public async Task Delete_with_invoicess_throws_business_rule()
    {
        _service.Setup(s => s.GetByIdAsync(5)).ReturnsAsync(new CustomerDto { Id = 5, CustomerCode = "C", CustomerName = "N" });
        _invoices.Setup(s => s.GetAllPagedAsync(null, 5, null, 1, 1))
            .ReturnsAsync(new PagedResultDto<SalesInvoiceDto> { TotalRecords = 2 });

        var handler = new DeleteCustomerHandler(_service.Object, _invoices.Object);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            handler.HandleAsync(new DeleteCustomerCommand(5), CancellationToken.None));

        _service.Verify(s => s.DeleteAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Delete_without_invoicess_calls_service()
    {
        _service.Setup(s => s.GetByIdAsync(5)).ReturnsAsync(new CustomerDto { Id = 5, CustomerCode = "C", CustomerName = "N" });
        _invoices.Setup(s => s.GetAllPagedAsync(null, 5, null, 1, 1))
            .ReturnsAsync(new PagedResultDto<SalesInvoiceDto> { TotalRecords = 0 });
        _service.Setup(s => s.DeleteAsync(5)).ReturnsAsync(true);

        await new DeleteCustomerHandler(_service.Object, _invoices.Object)
            .HandleAsync(new DeleteCustomerCommand(5), CancellationToken.None);

        _service.Verify(s => s.DeleteAsync(5), Times.Once);
    }
}
