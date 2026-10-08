using Npgsql;

namespace VulnerableApiLab.Api;

// This data access method is not mapped to an HTTP route. Private notes remain
// closed until authentication and object-level authorization are implemented.
public static class NoteLookup
{
    public static async Task<IReadOnlyList<int>> FindIdsByTitleAsync(
        NpgsqlConnection connection,
        string title,
        CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT id FROM notes WHERE title = $1 ORDER BY id";
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.Add(new NpgsqlParameter { Value = title });

        var ids = new List<int>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            ids.Add(reader.GetInt32(0));
        }

        return ids;
    }
}
