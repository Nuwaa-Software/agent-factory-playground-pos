using Npgsql;
using Testcontainers.PostgreSql;

namespace MiniPos.Tests.Integration;

/// <summary>
/// A fresh database for a test class.
/// If MINIPOS_TEST_DB is set (a server connection string, e.g. the Agent Factory sandbox's sidecar Postgres),
/// a uniquely named database is created on that server. Otherwise a Testcontainers Postgres is started
/// (needs Docker — the normal developer/CI path, like nuwaa-pos).
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;

    public string ConnectionString { get; private set; } = "";

    public async Task InitializeAsync()
    {
        var server = Environment.GetEnvironmentVariable("MINIPOS_TEST_DB");
        if (string.IsNullOrWhiteSpace(server))
        {
            _container = new PostgreSqlBuilder().WithImage("postgres:17").Build();
            await _container.StartAsync();
            server = _container.GetConnectionString();
        }

        var builder = new NpgsqlConnectionStringBuilder(server) { Database = $"minipos_test_{Guid.NewGuid():N}" };
        ConnectionString = builder.ConnectionString;
    }

    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }
}
