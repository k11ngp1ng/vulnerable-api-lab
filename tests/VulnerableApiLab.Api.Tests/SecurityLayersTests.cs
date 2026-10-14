using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace VulnerableApiLab.Api.Tests;

public sealed class SecurityLayersTests
{
    [Fact]
    public async Task Development_error_does_not_return_exception_details()
    {
        const string diagnostic = "fictional-database-diagnostic-marker";
        var logs = new ConcurrentQueue<string>();
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.ConfigureLogging(logging => logging.AddProvider(new RecordingLoggerProvider(logs)));
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<LabAuth>();
                    services.AddSingleton<LabAuth>(_ => throw new InvalidOperationException(diagnostic));
                });
            });
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/auth/register",
            new { username = "lab-user", password = "fictional-password-123" });
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.DoesNotContain(diagnostic, body, StringComparison.Ordinal);
        Assert.DoesNotContain("InvalidOperationException", body, StringComparison.Ordinal);
        Assert.DoesNotContain("fictional-password-123", body, StringComparison.Ordinal);
        Assert.DoesNotContain(logs, message => message.Contains(diagnostic, StringComparison.Ordinal));
        Assert.Contains(logs, message => message.Contains("internal error", StringComparison.Ordinal));
    }

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
            Exception? exception, Func<TState, Exception?, string> formatter) =>
            messages.Enqueue(formatter(state, exception) + (exception?.ToString() ?? ""));
    }
}
