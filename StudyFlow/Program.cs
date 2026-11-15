using Npgsql;
using StudyFlow.Components;
using StudyFlow.Services;

var builder = WebApplication.CreateBuilder(args);

// This demo has no login yet. Never expose the data API publicly before adding auth.
if (!builder.Environment.IsDevelopment())
{
    throw new InvalidOperationException(
        "StudyFlow demo backend is development-only until authentication and authorization are implemented.");
}

// Connection string is stored in dotnet user-secrets or an environment variable,
// never in source code. Npgsql connects to Supabase's PostgreSQL server from .NET.
builder.Services.AddSingleton<NpgsqlDataSource>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var connectionString = config.GetConnectionString("Supabase");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException(
            "Missing Supabase connection string. Set ConnectionStrings:Supabase using dotnet user-secrets (see README_RU.md).");
    }

    return new NpgsqlDataSourceBuilder(connectionString).Build();
});

builder.Services.AddScoped<StudyRepository>();
builder.Services.AddScoped<TaskState>();
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

var app = builder.Build();

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAntiforgery();

// REST backend routes are separate from Blazor frontend routes.
app.MapStudyFlowApi();
app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();
