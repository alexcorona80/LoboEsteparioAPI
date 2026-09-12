using DuckDB.NET.Data;
using LoboEsteparioAPI.Options;
using Microsoft.Extensions.Options;

namespace LoboEsteparioAPI.Data;

public sealed class DuckDbConnectionFactory : IDuckDbConnectionFactory
{
    private static readonly SemaphoreSlim InstallLock = new(1, 1);
    private static bool _extensionsInstalled;

    private readonly OvertureOptions _options;

    public DuckDbConnectionFactory(IOptions<OvertureOptions> options)
    {
        _options = options.Value;
    }

    public async Task<DuckDBConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new DuckDBConnection("DataSource=:memory:");
        await connection.OpenAsync(cancellationToken);

        await EnsureExtensionsInstalledAsync(connection, cancellationToken);

        await ExecuteAsync(connection, "LOAD httpfs;", cancellationToken);
        await ExecuteAsync(connection, "LOAD spatial;", cancellationToken);
        await ExecuteAsync(connection, $"SET s3_region='{_options.S3Region}';", cancellationToken);

        return connection;
    }

    // INSTALL descarga los binarios de la extensión al caché local de DuckDB la primera vez;
    // los LOAD siguientes (uno por conexión) son baratos, así que INSTALL solo se paga una vez por proceso.
    private static async Task EnsureExtensionsInstalledAsync(DuckDBConnection connection, CancellationToken cancellationToken)
    {
        if (_extensionsInstalled)
        {
            return;
        }

        await InstallLock.WaitAsync(cancellationToken);
        try
        {
            if (_extensionsInstalled)
            {
                return;
            }

            await ExecuteAsync(connection, "INSTALL httpfs;", cancellationToken);
            await ExecuteAsync(connection, "INSTALL spatial;", cancellationToken);
            _extensionsInstalled = true;
        }
        finally
        {
            InstallLock.Release();
        }
    }

    private static async Task ExecuteAsync(DuckDBConnection connection, string sql, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
