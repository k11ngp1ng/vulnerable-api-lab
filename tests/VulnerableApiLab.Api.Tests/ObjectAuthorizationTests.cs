using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Npgsql;
using Xunit;

namespace VulnerableApiLab.Api.Tests;

public sealed class ObjectAuthorizationTests
{
    [LocalPostgresFact]
    public async Task Authenticated_user_can_read_only_their_own_note()
    {
        const string password = "fictional-password-123";
        var firstName = $"lab-{Guid.NewGuid():N}"[..20];
        var secondName = $"lab-{Guid.NewGuid():N}"[..20];
        var firstNoteId = Random.Shared.Next(1_000_000, int.MaxValue);
        var secondNoteId = -Random.Shared.Next(1_000_000, int.MaxValue);
        string? firstUserId = null;
        string? secondUserId = null;

        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseEnvironment("Development"));
        using var firstClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        using var secondClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        using var anonymousClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        try
        {
            firstUserId = await RegisterAndLoginAsync(firstClient, firstName, password);
            secondUserId = await RegisterAndLoginAsync(secondClient, secondName, password);
            await InsertNoteAsync(firstNoteId, firstUserId, "Private note A");
            await InsertNoteAsync(secondNoteId, secondUserId, "Private note B");

            // Deliberately unsafe comparison, only in this test: authentication is
            // valid, but an ID-only read omits the owner's authorization decision.
            Assert.Equal("Private note A", await ReadWithoutOwnerCheckForExerciseAsync(firstNoteId, secondUserId));

            using var ownerRead = await firstClient.GetAsync($"/api/notes/{firstNoteId}");
            var owned = await ownerRead.Content.ReadFromJsonAsync<NoteResponse>();
            Assert.Equal(HttpStatusCode.OK, ownerRead.StatusCode);
            Assert.Equal(firstNoteId, owned?.Id);
            Assert.Equal("Private note A", owned?.Content);

            using var secondOwnerRead = await secondClient.GetAsync($"/api/notes/{secondNoteId}");
            Assert.Equal(HttpStatusCode.OK, secondOwnerRead.StatusCode);

            using var otherUserRead = await secondClient.GetAsync($"/api/notes/{firstNoteId}?ownerId={firstUserId}");
            Assert.Equal(HttpStatusCode.NotFound, otherUserRead.StatusCode);
            Assert.Empty(await otherUserRead.Content.ReadAsStringAsync());

            using var forgedHeader = new HttpRequestMessage(HttpMethod.Get, $"/api/notes/{firstNoteId}");
            forgedHeader.Headers.TryAddWithoutValidation("X-Lab-User", firstUserId);
            using var forgedRead = await secondClient.SendAsync(forgedHeader);
            Assert.Equal(HttpStatusCode.NotFound, forgedRead.StatusCode);

            using var anonymousRead = await anonymousClient.GetAsync($"/api/notes/{firstNoteId}");
            Assert.Equal(HttpStatusCode.Unauthorized, anonymousRead.StatusCode);

            // The other verbs are still closed, including for an owner.
            using var writeRequest = await firstClient.PutAsJsonAsync($"/api/notes/{firstNoteId}", new { content = "changed" });
            Assert.Equal(HttpStatusCode.Forbidden, writeRequest.StatusCode);
            Assert.Equal("Private note A", await ReadWithoutOwnerCheckForExerciseAsync(firstNoteId, firstUserId));
        }
        finally
        {
            await DeleteNoteAsync(firstNoteId);
            await DeleteNoteAsync(secondNoteId);
            if (firstUserId is not null) await DeleteUserAsync(firstUserId);
            if (secondUserId is not null) await DeleteUserAsync(secondUserId);
        }
    }

    private static async Task<string> RegisterAndLoginAsync(HttpClient client, string username, string password)
    {
        using var register = await client.PostAsJsonAsync("/api/auth/register", new { username, password });
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);
        var user = await register.Content.ReadFromJsonAsync<UserResponse>();
        Assert.False(string.IsNullOrWhiteSpace(user?.Id));
        using var login = await client.PostAsJsonAsync("/api/auth/login", new { username, password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        return user!.Id;
    }

    private static async Task InsertNoteAsync(int id, string ownerId, string content)
    {
        await using var connection = await OpenConnectionAsync();
        await using var command = new NpgsqlCommand("INSERT INTO notes (id, owner_id, title, content) VALUES ($1, $2, $3, $4)", connection);
        command.Parameters.Add(new NpgsqlParameter { Value = id });
        command.Parameters.Add(new NpgsqlParameter { Value = ownerId });
        command.Parameters.Add(new NpgsqlParameter { Value = "Fictional private note" });
        command.Parameters.Add(new NpgsqlParameter { Value = content });
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<string?> ReadWithoutOwnerCheckForExerciseAsync(int id, string authenticatedUserId)
    {
        // Deliberately omitted from the query: this is the BOLA/IDOR mistake.
        _ = authenticatedUserId;
        await using var connection = await OpenConnectionAsync();
        await using var command = new NpgsqlCommand("SELECT content FROM notes WHERE id = $1", connection);
        command.Parameters.Add(new NpgsqlParameter { Value = id });
        return (string?)await command.ExecuteScalarAsync();
    }

    private static async Task DeleteNoteAsync(int id)
    {
        await using var connection = await OpenConnectionAsync();
        await using var command = new NpgsqlCommand("DELETE FROM notes WHERE id = $1", connection);
        command.Parameters.Add(new NpgsqlParameter { Value = id });
        await command.ExecuteNonQueryAsync();
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

    private sealed record UserResponse(string Id, string Username);
    private sealed record NoteResponse(int Id, string Title, string Content);
}
