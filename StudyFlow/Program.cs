
using StudyFlow.Components;
using StudyFlow.Services;

var builder = WebApplication.CreateBuilder(args);

// Backend: shared data storage
builder.Services.AddSingleton<StudyDataStore>();

// Frontend: state management
builder.Services.AddScoped<TaskState>();
builder.Services.AddScoped<MockExamState>();

// Blazor
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Error handling
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

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
