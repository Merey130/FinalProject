using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace StudyFlow.Services;

// Calls Supabase PostgREST with the logged-in user's JWT so PostgreSQL RLS is enforced.
public sealed class SupabaseDatabase(IHttpClientFactory factory, SupabaseOptions options)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<List<T>> Get<T>(string path, string token)
    {
        using var r = await Send(HttpMethod.Get, path, token, null, false);
        return await r.Content.ReadFromJsonAsync<List<T>>(Json) ?? [];
    }
    public async Task<T> Insert<T>(string path, string token, object body)
    {
        using var r = await Send(HttpMethod.Post, path, token, body, true);
        var rows = await r.Content.ReadFromJsonAsync<List<T>>(Json);
        if (rows is null || rows.Count == 0)
    throw new InvalidOperationException("Supabase returned no row.");

return rows[0];
    }
    public async Task<List<T>> Patch<T>(string path, string token, object body)
    {
        using var r = await Send(HttpMethod.Patch, path, token, body, true);
        return await r.Content.ReadFromJsonAsync<List<T>>(Json) ?? [];
    }
    public async Task Delete(string path, string token)
    {
        using var _ = await Send(HttpMethod.Delete, path, token, null, false);
    }
    private async Task<HttpResponseMessage> Send(HttpMethod method, string path, string token, object? body, bool returning)
    {
        using var req = new HttpRequestMessage(method, options.UriFor("rest/v1/" + path));
        req.Headers.Add("apikey", options.PublishableKey);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (returning) req.Headers.Add("Prefer", "return=representation");
        if (body is not null) req.Content = JsonContent.Create(body, options: Json);
        var res = await factory.CreateClient("supabase").SendAsync(req);
        if (res.IsSuccessStatusCode) return res;
        var status = (int)res.StatusCode; res.Dispose();
        throw new HttpRequestException($"Supabase database request failed (HTTP {status}).");
    }
}
