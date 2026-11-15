using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace StudyFlow.Services;

public sealed class SupabaseAuth(IHttpClientFactory factory, SupabaseOptions options)
{
    private HttpRequestMessage Request(HttpMethod method, string path)
    {
        var r = new HttpRequestMessage(method, options.UriFor(path));
        r.Headers.Add("apikey", options.PublishableKey);
        return r;
    }

    public async Task<bool> Register(string email, string password, string name)
    {
        using var r = Request(HttpMethod.Post, "auth/v1/signup");
        r.Content = JsonContent.Create(new { email, password, data = new { display_name = name } });
        using var response = await factory.CreateClient("supabase").SendAsync(r);
        return response.IsSuccessStatusCode;
    }

    public async Task<AuthResult?> Login(string email, string password)
    {
        using var r = Request(HttpMethod.Post, "auth/v1/token?grant_type=password");
        r.Content = JsonContent.Create(new { email, password });
        using var response = await factory.CreateClient("supabase").SendAsync(r);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<AuthResult>();
    }

    public async Task<AuthResult?> Refresh(string refreshToken)
    {
        using var r = Request(HttpMethod.Post, "auth/v1/token?grant_type=refresh_token");
        r.Content = JsonContent.Create(new { refresh_token = refreshToken });
        using var response = await factory.CreateClient("supabase").SendAsync(r);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<AuthResult>();
    }

    public async Task Logout(string token)
    {
        using var r = Request(HttpMethod.Post, "auth/v1/logout");
        r.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var _ = await factory.CreateClient("supabase").SendAsync(r);
    }
}

public sealed class AuthResult
{
    [JsonPropertyName("access_token")] public string AccessToken { get; set; } = "";
    [JsonPropertyName("refresh_token")] public string RefreshToken { get; set; } = "";
    [JsonPropertyName("expires_in")] public int ExpiresIn { get; set; } = 3600;
    [JsonPropertyName("user")] public AuthUser User { get; set; } = new();
}
public sealed class AuthUser
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("email")] public string Email { get; set; } = "";
}
