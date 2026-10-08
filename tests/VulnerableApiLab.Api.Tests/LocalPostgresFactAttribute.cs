using Xunit;

namespace VulnerableApiLab.Api.Tests;

public sealed class LocalPostgresFactAttribute : FactAttribute
{
    public LocalPostgresFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("LAB_DB_CONNECTION_STRING")))
        {
            Skip = "Set LAB_DB_CONNECTION_STRING to run the local PostgreSQL experiment.";
        }
    }
}
