using DuckDB.NET.Data;

namespace LoboEsteparioAPI.Data;

public interface IDuckDbConnectionFactory
{
    /// <summary>
    /// Abre una nueva conexión DuckDB en memoria con las extensiones httpfs y spatial
    /// instaladas/cargadas y la región S3 configurada, lista para consultar los parquet
    /// remotos de Overture.
    /// </summary>
    Task<DuckDBConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default);
}
