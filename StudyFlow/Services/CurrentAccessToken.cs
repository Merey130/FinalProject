using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace StudyFlow.Services;

public sealed class CurrentAccessToken(AuthenticationStateProvider state, SessionStore sessions, SupabaseAuth auth)
{
    public async Task<string> Get()
    {
        var user = (await state.GetAuthenticationStateAsync()).User;
        var sid = user.FindFirst(SessionStore.Claim)?.Value;
        var token = await sessions.AccessToken(sid, auth);
        return token ?? throw new UnauthorizedAccessException("Please sign in again.");
    }
}
