using Npgsql;
using VulnerableApiLab.Api;
using Xunit;

namespace VulnerableApiLab.Api.Tests;

public sealed class SqlInjectionTests
{
    [LocalPostgresFact]
    public async Task Concatenated_input_changes_the_predicate_but_parameterized_input_does_not()
    {
        await using var connection = await OpenLocalConnectionAsync();
        const string input = "' OR 1=1 --";

        var unsafeIds = await FindIdsWithConcatenationAsync(connection, input);
        var safeIds = await NoteLookup.FindIdsByTitleAsync(connection, input);

        Assert.Equal([1, 2], unsafeIds.Order().ToArray());
        Assert.Empty(safeIds);
    }

    [LocalPostgresFact]
    public async Task Normal_title_returns_the_same_single_note_in_both_queries()
    {
        await using var connection = await OpenLocalConnectionAsync();
        const string input = "Lab note A";

        var unsafeIds = await FindIdsWithConcatenationAsync(connection, input);
        var safeIds = await NoteLookup.FindIdsByTitleAsync(connection, input);

        Assert.Equal([1], unsafeIds);
        Assert.Equal([1], safeIds);
    }

    // Deliberately vulnerable demonstration. Test-only: never register as an API route.
    private static async Task<IReadOnlyList<int>> FindIdsWithConcatenationAsync(
        NpgsqlConnection connection,
        string title)
    {
        var sql = $"SELECT id FROM notes WHERE title = '{title}' ORDER BY id";
        await using var command = new NpgsqlCommand(sql, connection);
        var ids = new List<int>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            ids.Add(reader.GetInt32(0));
        }

        return ids;
    }

    private static async Task<NpgsqlConnection> OpenLocalConnectionAsync()
    {
        var connectionString = Environment.GetEnvironmentVariable("LAB_DB_CONNECTION_STRING")
            ?? throw new InvalidOperationException("LAB_DB_CONNECTION_STRING is required.");
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        if (builder.Host != "127.0.0.1" || builder.Port != 5433 ||
            builder.Database != "vulnerable_api_lab" || builder.Username != "lab_user")
        {
            throw new InvalidOperationException("The experiment requires the local lab PostgreSQL connection.");
        }

        var connection = new NpgsqlConnection(builder.ConnectionString);
        await connection.OpenAsync();
        return connection;
    }
}
