using Asp.Versioning;
using Invoice.CqrsAPI.Common;
using Invoice.CqrsAPI.Cqrs;
using Invoice.CqrsAPI.Extensions;
using Invoice.CqrsAPI.Infrastructure;
using Invoice.CqrsAPI.Middleware;
using Microsoft.OpenApi;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.File("logs/cqrs-log.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog();

// ============================================================
// Controllers + API versioning (same URL style as Invoice.CoreAPI)
// ============================================================
builder.Services.AddControllers();

builder.Services
    .AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
    })
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

// ============================================================
// Existing Invoice.BAL / Invoice.DAL (no new BAL or DAL project)
// ============================================================
builder.Services.AddInvoiceDataAccess(builder.Configuration);
builder.Services.AddInvoiceBusinessServices();

// ============================================================
// CQRS: dispatcher + behaviors + every handler and validator in this assembly
// ============================================================
builder.Services.AddCqrs(typeof(Program).Assembly);
builder.Services.AddScoped<IReferenceChecks, ReferenceChecks>();   // shared "does it exist?" checks

// ============================================================
// JWT
// ============================================================
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();

// ============================================================
// Swagger
// ============================================================
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Invoice CQRS API",
        Version = "v1",
        Description = "Invoice Management - CQRS (commands + queries) over Invoice.BAL"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token."
    });

    options.AddSecurityRequirement(document =>
        new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = []
        });
});

// ============================================================
// CORS (Angular / React / Blazor ... clients)
// ============================================================
const string CorsPolicy = "_allowClients";

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicy, policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// ============================================================
// Pipeline
// ============================================================
var app = builder.Build();

app.UseGlobalExceptionHandling();   // must be first: turns exceptions into JSON responses

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Invoice CQRS API V1");
    options.RoutePrefix = "swagger";
});

app.UseCors(CorsPolicy);

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok());
app.MapControllers();

app.Run();

// Makes Program visible to integration tests (WebApplicationFactory<Program>).
public partial class Program { }
