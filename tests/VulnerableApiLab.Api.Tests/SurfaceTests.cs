using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace VulnerableApiLab.Api.Tests;

public sealed class SurfaceTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public SurfaceTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Production"));
    }

    [Fact]
    public async Task Health_is_available_without_exposing_private_data()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/health");
        var body = await response.Content.ReadFromJsonAsync<HealthResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("ok", body?.Status);
    }

    [Theory]
    [InlineData("/notes")]
    [InlineData("/notes/1")]
    [InlineData("/api/notes/1")]
    public async Task Private_note_routes_are_not_exposed_before_authorization_exists(string path)
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private sealed record HealthResponse(string Status);
}
