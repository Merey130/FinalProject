namespace StudyFlow.Services;

public sealed class SupabaseOptions
{
    public string Url { get; init; } = "";
    public string PublishableKey { get; init; } = "";
    public bool Configured => Uri.TryCreate(Url, UriKind.Absolute, out var u)
        && u.Scheme == Uri.UriSchemeHttps && !string.IsNullOrWhiteSpace(PublishableKey);
    public Uri UriFor(string path)
    {
        if (!Configured) throw new InvalidOperationException("Configure Supabase:Url and Supabase:PublishableKey.");
        return new Uri(new Uri(Url.TrimEnd('/') + "/"), path.TrimStart('/'));
    }
}
