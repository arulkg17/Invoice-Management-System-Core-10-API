using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Invoice.DTOs;
using Invoice.Model;
using Microsoft.Extensions.Configuration;

namespace Invoice.Functions.Services;

/// <summary>Keeps the JWT between function invocations (the HttpClient wrapper below is created per use).</summary>
public sealed class ImsTokenCache
{
    internal SemaphoreSlim Gate { get; } = new(1, 1);
    internal string? Token { get; set; }
    internal DateTime ExpiresUtc { get; set; } = DateTime.MinValue;
}

/// <summary>
/// Talks to Invoice.CqrsAPI like any other client: logs in with a service account, then calls the endpoints.
/// The Functions never reference the database or the BAL, so the API stays the single owner of the business rules.
/// </summary>
public sealed class ImsApiClient
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly ImsTokenCache _tokens;
    private readonly IConfiguration _configuration;

    public ImsApiClient(HttpClient http, ImsTokenCache tokens, IConfiguration configuration)
    {
        _http = http;
        _tokens = tokens;
        _configuration = configuration;
    }

    public Task<SalesInvoiceDto> GetInvoiceAsync(int id, CancellationToken ct = default) =>
        GetDataAsync<SalesInvoiceDto>($"api/v1/SalesInvoice/GetById/{id}", ct);

    public Task<CustomerDto> GetCustomerAsync(int id, CancellationToken ct = default) =>
        GetDataAsync<CustomerDto>($"api/v1/Customer/GetById/{id}", ct);

    public Task<ItemmasterDto> GetItemAsync(int id, CancellationToken ct = default) =>
        GetDataAsync<ItemmasterDto>($"api/v1/Itemmaster/GetById/{id}", ct);

    public Task<ItemStockDto> GetStockAsync(int itemmasterId, CancellationToken ct = default) =>
        GetDataAsync<ItemStockDto>($"api/v1/Stock/GetByItem/{itemmasterId}", ct);

    public async Task<IReadOnlyList<SalesInvoiceDto>> GetPostedInvoicesAsync(CancellationToken ct = default)
    {
        var page = await GetDataAsync<PagedResultDto<SalesInvoiceDto>>(
            "api/v1/SalesInvoice/GetAllPaged?status=Posted&pageNumber=1&pageSize=500", ct);

        return page.Data.ToList();
    }

    public async Task<byte[]> GetInvoicePdfAsync(int id, CancellationToken ct = default)
    {
        using var request = await CreateRequestAsync(HttpMethod.Get, $"api/v1/SalesInvoice/{id}/pdf", ct);
        using var response = await _http.SendAsync(request, ct);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsByteArrayAsync(ct);
    }

    // ---------------------------------------------------------------------------------

    private async Task<T> GetDataAsync<T>(string url, CancellationToken ct)
    {
        using var request = await CreateRequestAsync(HttpMethod.Get, url, ct);
        using var response = await _http.SendAsync(request, ct);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException($"IMS API {(int)response.StatusCode} for {url}: {body}", null, response.StatusCode);
        }

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(Json, ct);

        return envelope is { Success: true, Data: not null }
            ? envelope.Data
            : throw new InvalidOperationException($"IMS API returned no data for {url}: {envelope?.Message}");
    }

    private async Task<HttpRequestMessage> CreateRequestAsync(HttpMethod method, string url, CancellationToken ct)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await GetTokenAsync(ct));
        return request;
    }

    private async Task<string> GetTokenAsync(CancellationToken ct)
    {
        // Reuse the token until one minute before it expires.
        if (_tokens.Token is not null && _tokens.ExpiresUtc > DateTime.UtcNow.AddMinutes(1))
        {
            return _tokens.Token;
        }

        await _tokens.Gate.WaitAsync(ct);
        try
        {
            if (_tokens.Token is not null && _tokens.ExpiresUtc > DateTime.UtcNow.AddMinutes(1))
            {
                return _tokens.Token;
            }

            var login = new LoginRequestDto
            {
                UserName = _configuration["ImsApi:UserName"] ?? throw new InvalidOperationException("ImsApi:UserName is not configured."),
                Password = _configuration["ImsApi:Password"] ?? throw new InvalidOperationException("ImsApi:Password is not configured.")
            };

            using var response = await _http.PostAsJsonAsync("api/v1/Login", login, Json, ct);
            response.EnsureSuccessStatusCode();

            var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponseDto>>(Json, ct);
            var result = envelope?.Data ?? throw new InvalidOperationException("Login returned no data.");

            _tokens.Token = result.Token;
            _tokens.ExpiresUtc = result.Expiration.ToUniversalTime();

            return result.Token;
        }
        finally
        {
            _tokens.Gate.Release();
        }
    }
}
