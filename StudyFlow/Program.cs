using StudyFlow.Components;
using StudyFlow.Services;

var builder = WebApplication.CreateBuilder(args);

// One shared data store: assignments, students and practice exams are saved to disk.
builder.Services.AddSingleton<StudyDataStore>();

// Compatibility with the existing Home.razor and MyTasks.razor pages.
builder.Services.AddScoped<TaskState>();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAntiforgery();

// Backend API; Blazor frontend routes are still handled below.
app.MapStudyFlowApi();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
