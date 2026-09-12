using LoboEsteparioAPI.Data;
using LoboEsteparioAPI.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace LoboEsteparioAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class HealthController(IDuckDbConnectionFactory connectionFactory, IOptions<OvertureOptions> options) : ControllerBase
{
    /// <summary>Chequeo básico de que la API está viva.</summary>
    [HttpGet]
    public ActionResult<object> Get() => Ok(new { status = "healthy" });

    /// <summary>
    /// Verifica que DuckDB pueda cargar las extensiones httpfs/spatial y alcanzar el release
    /// de Overture configurado, sin escanear el dataset completo.
    /// </summary>
    [HttpGet("overture")]
    public async Task<ActionResult<object>> CheckOvertureAsync(CancellationToken cancellationToken)
    {
        var overtureOptions = options.Value;

        try
        {
            await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = $"SELECT id FROM read_parquet('{overtureOptions.BuildDatasetPath()}', filename=true, hive_partitioning=1) LIMIT 1;";

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var reachable = await reader.ReadAsync(cancellationToken);

            return Ok(new
            {
                status = reachable ? "reachable" : "empty",
                overtureOptions.ReleaseVersion,
                overtureOptions.Theme,
                overtureOptions.Type,
                datasetPath = overtureOptions.BuildDatasetPath(),
            });
        }
        catch (Exception ex)
        {
            return Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "No se pudo alcanzar el dataset de Overture");
        }
    }
}
