using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;
using Npgsql;
using Xunit;

namespace VulnerableApiLab.Api.Tests;

public sealed class AuthenticationTests
{
    [Fact]
    public async Task Authentication_routes_are_absent_in_production()
    {
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseEnvironment("Production"));
        using var client = factory.CreateClient();

        using var register = await client.PostAsJsonAsync("/api/auth/register", new { username = "test-user", password = "fictional-password-123" });
        using var login = await client.PostAsJsonAsync("/api/auth/login", new { username = "test-user", password = "fictional-password-123" });
        using var me = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.NotFound, register.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, login.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, me.StatusCode);
    }

    [LocalPostgresFact]
    public async Task Registration_stores_salted_hash_and_login_only_accepts_correct_password()
    {
        const string password = "fictional-password-123";
        const string wrongPasswordValue = "fictional-password-456";
        var firstName = $"lab-{Guid.NewGuid():N}"[..20];
        var secondName = $"lab-{Guid.NewGuid():N}"[..20];
        var logs = new ConcurrentQueue<string>();
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.ConfigureLogging(logging => logging.AddProvider(new RecordingLoggerProvider(logs)));
            });
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });

        // Deliberately unsafe comparison, kept only in this test: plaintext remains recoverable.
        var unsafeRecord = new Dictionary<string, string> { [firstName] = password };
        Assert.Equal(password, unsafeRecord[firstName]);

        string? firstId = null;
        string? secondId = null;
        try
        {
            using var firstRegister = await client.PostAsJsonAsync("/api/auth/register", new { username = firstName, password });
            Assert.Equal(HttpStatusCode.Created, firstRegister.StatusCode);
            firstId = (await firstRegister.Content.ReadFromJsonAsync<LabUserResponse>())?.Id;
            Assert.False(string.IsNullOrWhiteSpace(firstId));

            using var secondRegister = await client.PostAsJsonAsync("/api/auth/register", new { username = secondName, password });
            Assert.Equal(HttpStatusCode.Created, secondRegister.StatusCode);
            secondId = (await secondRegister.Content.ReadFromJsonAsync<LabUserResponse>())?.Id;
            Assert.False(string.IsNullOrWhiteSpace(secondId));

            var firstHash = await ReadHashAsync(firstId!);
            var secondHash = await ReadHashAsync(secondId!);
            Assert.NotEqual(password, firstHash);
            Assert.DoesNotContain(password, firstHash);
            Assert.NotEqual(firstHash, secondHash);

            using var anonymousMe = await client.GetAsync("/api/auth/me");
            Assert.Equal(HttpStatusCode.Unauthorized, anonymousMe.StatusCode);

            using var wrongPassword = await client.PostAsJsonAsync("/api/auth/login", new { username = firstName, password = wrongPasswordValue });
            Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
            Assert.False(wrongPassword.Headers.Contains("Set-Cookie"));

            using var correctPassword = await client.PostAsJsonAsync("/api/auth/login", new { username = firstName, password });
            Assert.Equal(HttpStatusCode.OK, correctPassword.StatusCode);
            Assert.True(correctPassword.Headers.Contains("Set-Cookie"));

            using var authenticatedMe = await client.GetAsync("/api/auth/me");
            var identity = await authenticatedMe.Content.ReadFromJsonAsync<LabUserResponse>();
            Assert.Equal(HttpStatusCode.OK, authenticatedMe.StatusCode);
            Assert.Equal(firstId, identity?.Id);
            Assert.Equal(firstName, identity?.Username);

            using var privateNote = await client.GetAsync("/api/notes/1");
            Assert.Equal(HttpStatusCode.NotFound, privateNote.StatusCode);
            Assert.DoesNotContain(logs, message => message.Contains(password, StringComparison.Ordinal));
            Assert.DoesNotContain(logs, message => message.Contains(wrongPasswordValue, StringComparison.Ordinal));
        }
        finally
        {
            if (firstId is not null) await DeleteUserAsync(firstId);
            if (secondId is not null) await DeleteUserAsync(secondId);
        }
    }

    private static async Task<string> ReadHashAsync(string id)
    {
        await using var connection = await OpenConnectionAsync();
        await using var command = new NpgsqlCommand("SELECT password_hash FROM lab_users WHERE id = $1", connection);
        command.Parameters.Add(new NpgsqlParameter { Value = id });
        return (string)(await command.ExecuteScalarAsync())!;
    }

    private static async Task DeleteUserAsync(string id)
    {
        await using var connection = await OpenConnectionAsync();
        await using var command = new NpgsqlCommand("DELETE FROM lab_users WHERE id = $1", connection);
        command.Parameters.Add(new NpgsqlParameter { Value = id });
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<NpgsqlConnection> OpenConnectionAsync()
    {
        var connection = new NpgsqlConnection(Environment.GetEnvironmentVariable("LAB_DB_CONNECTION_STRING"));
        await connection.OpenAsync();
        return connection;
    }

    private sealed record LabUserResponse(string Id, string Username);

    private sealed class RecordingLoggerProvider(ConcurrentQueue<string> messages) : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => new RecordingLogger(messages);
        public void Dispose() { }
    }

    private sealed class RecordingLogger(ConcurrentQueue<string> messages) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter) => messages.Enqueue(formatter(state, exception));
    }
}
