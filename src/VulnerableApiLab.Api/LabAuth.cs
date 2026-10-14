using Microsoft.AspNetCore.Identity;
using Npgsql;

namespace VulnerableApiLab.Api;

public sealed record Credentials(string? Username, string? Password);
public sealed record LabUser(string Id, string Username);
public sealed record PrivateNote(int Id, string Title, string Content);

public sealed class LabAuth
{
    private readonly string _connectionString;
    private readonly IPasswordHasher<LabUser> _passwordHasher;

    public LabAuth(string connectionString, IPasswordHasher<LabUser> passwordHasher)
    {
        var settings = new NpgsqlConnectionStringBuilder(connectionString);
        if (settings.Host != "127.0.0.1" || settings.Port != 5433 ||
            settings.Database != "vulnerable_api_lab" || settings.Username != "lab_user")
        {
            throw new InvalidOperationException("Authentication requires the local lab PostgreSQL connection.");
        }

        _connectionString = settings.ConnectionString;
        _passwordHasher = passwordHasher;
    }

    public static bool TryNormalize(Credentials? credentials, out string username)
    {
        username = credentials?.Username?.Trim().ToLowerInvariant() ?? "";
        return username.Length is >= 3 and <= 32 &&
            username.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_') &&
            credentials?.Password is { Length: >= 12 and <= 128 };
    }

    public async Task<LabUser?> RegisterAsync(string username, string password, CancellationToken cancellationToken)
    {
        var user = new LabUser(Guid.NewGuid().ToString("N"), username);
        var hash = _passwordHasher.HashPassword(user, password);

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        const string sql = "INSERT INTO lab_users (id, display_name, login_name, password_hash) VALUES ($1, $2, $3, $4)";
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.Add(new NpgsqlParameter { Value = user.Id });
        command.Parameters.Add(new NpgsqlParameter { Value = username });
        command.Parameters.Add(new NpgsqlParameter { Value = username });
        command.Parameters.Add(new NpgsqlParameter { Value = hash });

        try
        {
            await command.ExecuteNonQueryAsync(cancellationToken);
            return user;
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.UniqueViolation &&
            exception.ConstraintName == "lab_users_login_name_key")
        {
            return null;
        }
    }

    public async Task<LabUser?> AuthenticateAsync(string username, string password, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        const string sql = "SELECT id, password_hash FROM lab_users WHERE login_name = $1";
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.Add(new NpgsqlParameter { Value = username });
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken) || reader.IsDBNull(1))
        {
            return null;
        }

        var user = new LabUser(reader.GetString(0), username);
        var result = _passwordHasher.VerifyHashedPassword(user, reader.GetString(1), password);
        return result == PasswordVerificationResult.Failed ? null : user;
    }

    public async Task<PrivateNote?> ReadOwnedNoteAsync(int id, string authenticatedUserId, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        // The owner constraint is part of the read itself. A known note ID is not permission.
        const string sql = "SELECT id, title, content FROM notes WHERE id = $1 AND owner_id = $2";
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.Add(new NpgsqlParameter { Value = id });
        command.Parameters.Add(new NpgsqlParameter { Value = authenticatedUserId });
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken)
            ? new PrivateNote(reader.GetInt32(0), reader.GetString(1), reader.GetString(2))
            : null;
    }
}
