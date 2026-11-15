
using Npgsql;
using StudyFlow.Components;
using StudyFlow.Services;

var builder = WebApplication.CreateBuilder(args);

// Demo-only until authentication is implemented
if (!builder.Environment.IsDevelopment())
{
    throw new InvalidOperationException(
        "StudyFlow is development-only until authentication is implemented.");
}

// Existing frontend storage
builder.Services.AddSingleton<StudyDataStore>();
builder.Services.AddScoped<TaskState>();
builder.Services.AddScoped<MockExamState>();

// New Supabase PostgreSQL backend
builder.Services.AddSingleton<NpgsqlDataSource>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();

    var connectionString =
        config.GetConnectionString("Supabase");

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException(
            "Missing Supabase connection string.");
    }

    return new NpgsqlDataSourceBuilder(connectionString)
        .Build();
});

builder.Services.AddScoped<StudyRepository>();

// Blazor components
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

app.UseStatusCodePagesWithReExecute(
    "/not-found",
    createScopeForStatusCodePages: true);

app.UseHttpsRedirection();
app.UseAntiforgery();

// Backend API
app.MapStudyFlowApi();

// Frontend
app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
