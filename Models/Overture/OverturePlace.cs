namespace LoboEsteparioAPI.Models.Overture;

/// <summary>Un negocio proyectado desde el dataset theme=places/type=place de Overture.</summary>
public sealed class OverturePlace
{
    public required string Id { get; init; }
    public string? Name { get; init; }

    /// <summary>Etiqueta de categoría simplificada ("basic_category" de Overture), ej. "restaurant".</summary>
    public string? MainCategory { get; init; }

    /// <summary>Categoría más específica dentro de la taxonomía completa de Overture ("taxonomy.primary").</summary>
    public string? DetailedCategory { get; init; }
    public IReadOnlyList<string> AlternateCategories { get; init; } = [];
    public double? Confidence { get; init; }
    public double? Longitude { get; init; }
    public double? Latitude { get; init; }
    public string? Address { get; init; }
    public string? Locality { get; init; }
    public string? Region { get; init; }
    public string? Country { get; init; }
    public string? PostCode { get; init; }
    public IReadOnlyList<string> Phones { get; init; } = [];
    public IReadOnlyList<string> Websites { get; init; } = [];
    public IReadOnlyList<string> Emails { get; init; } = [];
    public IReadOnlyList<string> Socials { get; init; } = [];
    public DateTimeOffset? UpdateTime { get; init; }
}
