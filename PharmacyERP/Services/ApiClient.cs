using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace PharmacyERP.Services;

public sealed class ApiClient
{
    private static readonly Lazy<ApiClient> _instance = new(() => new ApiClient());
    public static ApiClient Instance => _instance.Value;

    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public string? Token { get; private set; }

    private ApiClient()
    {
        var baseUrl = "http://localhost:5197";
        try
        {
            var cfgPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            if (File.Exists(cfgPath))
            {
                var json = File.ReadAllText(cfgPath);
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("Api", out var api) &&
                    api.TryGetProperty("BaseUrl", out var url) &&
                    url.GetString() is { Length: > 0 } u)
                    baseUrl = u;
            }
        }
        catch { /* fallback to default */ }

        _http = new HttpClient { BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/"), Timeout = TimeSpan.FromSeconds(30) };
    }

    public void SetToken(string token)
    {
        Token = token;
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public void ClearToken()
    {
        Token = null;
        _http.DefaultRequestHeaders.Authorization = null;
    }

    public async Task<LoginResult> LoginAsync(string username, string password, CancellationToken ct = default)
    {
        var res = await _http.PostAsJsonAsync("api/auth/login", new { username, password }, ct);
        var body = await res.Content.ReadAsStringAsync(ct);
        if (!res.IsSuccessStatusCode)
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(body) ? $"Login failed ({(int)res.StatusCode})." : body);
        var data = JsonSerializer.Deserialize<LoginResult>(body, _json)
            ?? throw new InvalidOperationException("Invalid login response.");
        SetToken(data.Token);
        return data;
    }

    public async Task<T?> GetAsync<T>(string url, CancellationToken ct = default)
        => await _http.GetFromJsonAsync<T>(url, _json, ct);

    public async Task<TResp?> PostAsync<TReq, TResp>(string url, TReq payload, CancellationToken ct = default)
    {
        var res = await _http.PostAsJsonAsync(url, payload, _json, ct);
        var body = await res.Content.ReadAsStringAsync(ct);
        if (!res.IsSuccessStatusCode) throw new InvalidOperationException(ApiError(res, body));
        if (string.IsNullOrWhiteSpace(body)) return default;
        return JsonSerializer.Deserialize<TResp>(body, _json);
    }

    public async Task<TResp?> PutAsync<TReq, TResp>(string url, TReq payload, CancellationToken ct = default)
    {
        var res = await _http.PutAsJsonAsync(url, payload, _json, ct);
        var body = await res.Content.ReadAsStringAsync(ct);
        if (!res.IsSuccessStatusCode) throw new InvalidOperationException(ApiError(res, body));
        if (string.IsNullOrWhiteSpace(body)) return default;
        return JsonSerializer.Deserialize<TResp>(body, _json);
    }

    public async Task DeleteAsync(string url, CancellationToken ct = default)
    {
        var res = await _http.DeleteAsync(url, ct);
        if (!res.IsSuccessStatusCode)
            throw new InvalidOperationException(ApiError(res, await res.Content.ReadAsStringAsync(ct)));
    }

    private static string ApiError(HttpResponseMessage res, string body)
        => string.IsNullOrWhiteSpace(body) ? $"Request failed ({(int)res.StatusCode} {res.ReasonPhrase})." : body;

    public record LoginResult(string Token, string Username, string FullName, string Role, DateTime ExpiresAt);
}
