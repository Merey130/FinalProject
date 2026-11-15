using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace StudyFlow.Services;

// In-memory demo session store. Server restart requires users to sign in again.
public sealed class SessionStore
{
    public const string Claim = "studyflow_sid";
    private readonly ConcurrentDictionary<string, Session> _items = new();

    public string Add(AuthResult a)
    {
        var id = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        _items[id] = new Session(a);
        return id;
    }
    public bool Exists(string? id) => id is not null && _items.ContainsKey(id);
    public void Remove(string? id) { if (id is not null) _items.TryRemove(id, out _); }

    public async Task<string?> AccessToken(string? id, SupabaseAuth auth)
    {
        if (id is null || !_items.TryGetValue(id, out var s)) return null;
        if (s.Expires > DateTimeOffset.UtcNow.AddMinutes(1)) return s.Access;
        await s.Lock.WaitAsync();
        try {
            if (s.Expires > DateTimeOffset.UtcNow.AddMinutes(1)) return s.Access;
            var next = await auth.Refresh(s.Refresh);
            if (next is null) { Remove(id); return null; }
            s.Access = next.AccessToken; s.Refresh = next.RefreshToken;
            s.Expires = DateTimeOffset.UtcNow.AddSeconds(Math.Max(60, next.ExpiresIn));
            return s.Access;
        } finally { s.Lock.Release(); }
    }

    private sealed class Session
    {
        public string Access, Refresh;
        public DateTimeOffset Expires;
        public SemaphoreSlim Lock = new(1, 1);
        public Session(AuthResult a) {
            Access = a.AccessToken; Refresh = a.RefreshToken;
            Expires = DateTimeOffset.UtcNow.AddSeconds(Math.Max(60, a.ExpiresIn));
        }
    }
}
