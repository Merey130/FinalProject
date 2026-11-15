using System.Text.Json;
using System.Text.Json.Serialization;

namespace StudyFlow.Services;

/// <summary>
/// Read-only views for a curator's explicitly assigned students. The queries
/// are run with the curator's own Supabase JWT; SQL RLS is the final boundary.
/// </summary>
public sealed class StudyFlowCuratorRepository(SupabaseDatabase database)
{
    public async Task<List<JsonElement>> GetStudentsAsync(string jwt, Guid curatorId)
    {
        var links = await database.Get<LinkRow>(
            $"sf_curator_students?select=student_id&curator_id=eq.{curatorId:D}", jwt);

        var ids = links.Select(x => x.StudentId)
                       .Where(x => x != Guid.Empty)
                       .Distinct()
                       .ToList();
        if (ids.Count == 0) return [];

        var filter = string.Join(",", ids.Select(x => x.ToString("D")));
        return await database.Get<JsonElement>(
            $"profiles?select=id,display_name&id=in.({filter})&order=display_name.asc", jwt);
    }

    public Task<List<JsonElement>> GetStudentTasksAsync(string jwt, Guid studentId) =>
        database.Get<JsonElement>(
            $"sf_tasks?select=id,title,subject,deadline,is_done&owner_id=eq.{studentId:D}&order=deadline.asc", jwt);

    public Task<List<JsonElement>> GetStudentExamsAsync(string jwt, Guid studentId) =>
        database.Get<JsonElement>(
            $"sf_practice_exams?select=id,title,taken_on&owner_id=eq.{studentId:D}&order=taken_on.desc", jwt);

    public Task<List<JsonElement>> GetStudentScoresAsync(string jwt, Guid studentId) =>
        database.Get<JsonElement>(
            $"sf_subject_scores?select=exam_id,subject,score,max_score&owner_id=eq.{studentId:D}", jwt);

    private sealed class LinkRow
    {
        [JsonPropertyName("student_id")]
        public Guid StudentId { get; set; }
    }
}
