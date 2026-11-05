using System.Data;
using Invoice.BAL.Contracts;
using Invoice.BAL.Mapper;
using Invoice.BAL.Services;
using Invoice.DAL.Contracts;
using Invoice.DAL.Repositories;
using Invoice.Data.Db;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Invoice.CqrsAPI.Extensions;

/// <summary>
/// Registers the EXISTING Invoice.BAL services and Invoice.DAL repositories, exactly as
/// Invoice.CoreAPI's Program.cs does. No new BAL or DAL is written for the CQRS API:
/// the handlers simply call these services.
///
/// Every master and transaction is registered up front, so adding a new feature later
/// (Customer, Vendor, Purchase Order ...) needs only new files under /Features.
/// </summary>
public static class BusinessServicesExtensions
{
    public static IServiceCollection AddInvoiceDataAccess(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("DefaultConnection is not configured.");
        }

        services.AddScoped<IDbConnection>(_ => new SqlConnection(connectionString));
        services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));

        return services;
    }

    public static IServiceCollection AddInvoiceBusinessServices(this IServiceCollection services)
    {
        services.AddAutoMapper(cfg =>
        {
            cfg.AddProfile<CategoryProfile>();
            cfg.AddProfile<ItemmasterProfile>();
            cfg.AddProfile<CustomerProfile>();
            cfg.AddProfile<VendorProfile>();
            cfg.AddProfile<UserProfile>();
            cfg.AddProfile<PurchaseOrderProfile>();
            cfg.AddProfile<ReceiptProfile>();
            cfg.AddProfile<SalesInvoiceProfile>();
            cfg.AddProfile<StockProfile>();
        });

        // ---- Masters ----
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<ICategoryService, CategoryService>();

        services.AddScoped<IItemmasterRepository, ItemmasterRepositoryEFSp>();
        services.AddScoped<IItemmasterService, ItemmasterServiceEFSp>();

        services.AddScoped<ICustomerRepository, CustomerRepositoryEFSp>();
        services.AddScoped<ICustomerService, CustomerServiceEFSp>();

        services.AddScoped<IVendorRepository, VendorRepositoryEFSp>();
        services.AddScoped<IVendorService, VendorServiceEFSp>();

        services.AddScoped<IUserRepository, UserRepositorySpDap>();
        services.AddScoped<IUserService, UserServiceSpDap>();

        // ---- Transactions ----
        services.AddScoped<ITransactionRunner, EfTransactionRunner>();

        services.AddScoped<IPurchaseOrderRepository, PurchaseOrderRepositoryEFSp>();
        services.AddScoped<IPurchaseOrderDetailRepository, PurchaseOrderDetailRepositoryEFSp>();
        services.AddScoped<IPurchaseOrderService, PurchaseOrderServiceEFSp>();

        services.AddScoped<IReceiptRepository, ReceiptRepositoryEFSp>();
        services.AddScoped<IReceiptDetailRepository, ReceiptDetailRepositoryEFSp>();
        services.AddScoped<IReceiptService, ReceiptServiceEFSp>();

        services.AddScoped<ISalesInvoiceRepository, SalesInvoiceRepositoryEFSp>();
        services.AddScoped<ISalesInvoiceDetailRepository, SalesInvoiceDetailRepositoryEFSp>();
        services.AddScoped<ISalesInvoiceService, SalesInvoiceServiceEFSp>();

        // ---- Read-only ----
        services.AddScoped<IStockRepository, StockRepositoryEFSp>();
        services.AddScoped<IStockService, StockServiceEFSp>();
        services.AddScoped<IMasterCountService, MasterCountService>();

        // ---- PDF printing (Purchase Order, Receipt, Sales Invoice) ----
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        services.AddSingleton<IDocumentPdfService, DocumentPdfService>();
        services.AddScoped<IDocumentPrintService, DocumentPrintService>();

        return services;
    }
}
