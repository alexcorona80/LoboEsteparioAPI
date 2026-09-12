namespace LoboEsteparioAPI.Models.Requests;

/// <summary>
/// Parámetros de consulta aceptados por GET /api/places. El bounding box es obligatorio porque
/// el tema "places" de Overture cubre todo el planeta — toda búsqueda debe acotarse a un área.
/// </summary>
public sealed class PlaceSearchRequest
{
    /// <summary>Longitud mínima (oeste) del bounding box de búsqueda.</summary>
    public required double MinLongitude { get; init; }

    /// <summary>Latitud mínima (sur) del bounding box de búsqueda.</summary>
    public required double MinLatitude { get; init; }

    /// <summary>Longitud máxima (este) del bounding box de búsqueda.</summary>
    public required double MaxLongitude { get; init; }

    /// <summary>Latitud máxima (norte) del bounding box de búsqueda.</summary>
    public required double MaxLatitude { get; init; }

    /// <summary>Filtro opcional sobre la etiqueta simplificada "basic_category" de Overture, ej. "restaurant".
    /// Ver https://docs.overturemaps.org/guides/places/ para la lista completa de categorías básicas.</summary>
    public string? Category { get; init; }

    /// <summary>Filtro opcional, sin distinguir mayúsculas, de subcadena sobre el nombre principal del negocio.</summary>
    public string? NameContains { get; init; }

    /// <summary>Cantidad máxima de resultados a devolver.</summary>
    public int? Limit { get; init; }

    public static PlaceSearchRequest FromQuery(
        double minLongitude, double minLatitude, double maxLongitude, double maxLatitude,
        string? category, string? nameContains, int? limit) => new()
    {
        MinLongitude = minLongitude,
        MinLatitude = minLatitude,
        MaxLongitude = maxLongitude,
        MaxLatitude = maxLatitude,
        Category = category,
        NameContains = nameContains,
        Limit = limit,
    };
}
