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

    [Fact]
    public async Task Anonymous_reader_cannot_access_a_private_note()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/api/notes/1");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("GET", "/api/notes/1?ownerId=user-a")]
    [InlineData("PUT", "/api/notes/1")]
    [InlineData("DELETE", "/api/notes/1")]
    [InlineData("POST", "/api/notes")]
    public async Task Client_supplied_identity_does_not_unlock_private_notes(string method, string path)
    {
        using var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        request.Headers.TryAddWithoutValidation("X-Lab-User", "user-a");
        if (method is "POST" or "PUT")
        {
            request.Content = JsonContent.Create(new { ownerId = "user-a", content = "changed" });
        }

        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Empty(body);
    }

    [Fact]
    public async Task Unregistered_note_path_stays_not_found()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/notes/1");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private sealed record HealthResponse(string Status);
}
