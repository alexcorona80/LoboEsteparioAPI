namespace LoboEsteparioAPI.Options;

/// <summary>
/// Configuración para ubicar y consultar el release abierto de Overture Maps a través de DuckDB.
/// </summary>
public sealed class OvertureOptions
{
    public const string SectionName = "Overture";

    /// <summary>Ruta base del contenedor del release, ej. "s3://overturemaps-us-west-2/release".</summary>
    public required string BaseUrl { get; set; }

    /// <summary>Identificador del release basado en fecha, ej. "2026-08-19.0". Ver https://docs.overturemaps.org/release-calendar/.</summary>
    public required string ReleaseVersion { get; set; }

    /// <summary>Tema de Overture a consultar. Los negocios viven bajo el tema "places".</summary>
    public string Theme { get; set; } = "places";

    /// <summary>Tipo de feature dentro del tema.</summary>
    public string Type { get; set; } = "place";

    /// <summary>Región de AWS donde vive el bucket S3 de Overture, requerida por la extensión httpfs de DuckDB.</summary>
    public string S3Region { get; set; } = "us-west-2";

    /// <summary>Cantidad de resultados devueltos cuando una búsqueda no especifica límite.</summary>
    public int DefaultResultLimit { get; set; } = 100;

    /// <summary>Tope máximo aplicado a toda búsqueda, sin importar el límite solicitado.</summary>
    public int MaxResultLimit { get; set; } = 1000;

    /// <summary>Construye el glob de read_parquet() para el theme/type/release configurados.</summary>
    public string BuildDatasetPath() =>
        $"{BaseUrl.TrimEnd('/')}/{ReleaseVersion}/theme={Theme}/type={Type}/*";
}
