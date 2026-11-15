using System.Text.Json.Serialization;

namespace StudyFlow.Services;

/// <summary>
/// Reads the role from an administrator-managed table using the signed-in
/// user's Supabase access token. Never trust registration form fields or
/// user-editable metadata as an authority for Curator access.
/// </summary>
public sealed class StudyFlowRoleService(SupabaseDatabase database)
{
    public const string Student = "Student";
    public const string Curator = "Curator";

    public async Task<string> GetRoleAsync(string accessToken, string userId)
    {
        if (!Guid.TryParse(userId, out var id))
            throw new InvalidOperationException("Invalid authenticated user ID.");

        var roles = await database.Get<RoleRow>(
            $"sf_user_roles?select=role&user_id=eq.{id:D}", accessToken);

        // Fail safely: never promote a missing/unknown role to Curator.
        return roles.FirstOrDefault()?.Role == Curator ? Curator : Student;
    }

    private sealed class RoleRow
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = "";
    }
}
