using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Npgsql;
using Xunit;

namespace VulnerableApiLab.Api.Tests;

public sealed class ApiAbuseTests
{
    [LocalPostgresFact]
    public async Task Login_rejects_excess_attempts_and_recovers_after_window()
    {
        const string password = "fictional-password-123";
        const string wrongPassword = "fictional-password-456";
        var username = $"lab-{Guid.NewGuid():N}"[..20];
        string? userId = null;
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseEnvironment("Development"));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        try
        {
            using var register = await client.PostAsJsonAsync("/api/auth/register", new { username, password });
            Assert.Equal(HttpStatusCode.Created, register.StatusCode);
            userId = (await register.Content.ReadFromJsonAsync<LabUserResponse>())?.Id;
            Assert.False(string.IsNullOrWhiteSpace(userId));

            // Isolated baseline: password verification alone accepts every attempt for processing.
            var auth = new LabAuth(Environment.GetEnvironmentVariable("LAB_DB_CONNECTION_STRING")!, new PasswordHasher<LabUser>());
            for (var attempt = 0; attempt < 4; attempt++)
            {
                Assert.Null(await auth.AuthenticateAsync(username, wrongPassword, CancellationToken.None));
            }

            for (var attempt = 0; attempt < 3; attempt++)
            {
                using var response = await client.PostAsJsonAsync("/api/auth/login", new { username, password = wrongPassword });
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            }

            using var limited = await client.PostAsJsonAsync("/api/auth/login", new { username, password });
            Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
            Assert.True(limited.Headers.RetryAfter?.Delta > TimeSpan.Zero);
            Assert.False(limited.Headers.Contains("Set-Cookie"));

            await Task.Delay(TimeSpan.FromSeconds(5.2));
            using var recovered = await client.PostAsJsonAsync("/api/auth/login", new { username, password });
            Assert.Equal(HttpStatusCode.OK, recovered.StatusCode);
            Assert.True(recovered.Headers.Contains("Set-Cookie"));
        }
        finally
        {
            if (userId is not null)
            {
                await using var connection = new NpgsqlConnection(Environment.GetEnvironmentVariable("LAB_DB_CONNECTION_STRING"));
                await connection.OpenAsync();
                await using var command = new NpgsqlCommand("DELETE FROM lab_users WHERE id = $1", connection);
                command.Parameters.Add(new NpgsqlParameter { Value = userId });
                await command.ExecuteNonQueryAsync();
            }
        }
    }

    private sealed record LabUserResponse(string Id, string Username);
}
