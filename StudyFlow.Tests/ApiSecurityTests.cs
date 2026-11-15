using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace StudyFlow.Tests;

public class ApiSecurityTests
{
    [Theory]
    [InlineData("/api/tasks")]
    [InlineData("/api/exams")]
    [InlineData("/api/progress")]
    [InlineData("/api/me")]
    public async Task ProtectedApi_RequiresLogin(string url)
    {
        using var app = new WebApplicationFactory<Program>();

        using var client = app.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                BaseAddress = new Uri("https://localhost")
            });

        using var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location;
        Assert.NotNull(location);

        var path = location.IsAbsoluteUri
            ? location.AbsolutePath
            : location.OriginalString.Split('?')[0];

        Assert.Equal("/login", path);
    }

    [Fact]
    public async Task HealthApi_ReturnsOk()
    {
        using var app = new WebApplicationFactory<Program>();

        using var client = app.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost")
            });

        using var response = await client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var json = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        Assert.Equal("ok",
            json.RootElement.GetProperty("status").GetString());
    }
}
