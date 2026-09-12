namespace LoboEsteparioAPI.Models.Responses;

/// <summary>Resultado de ejecutar una búsqueda en Overture y persistir sus coincidencias en MongoDB.</summary>
public sealed class ScrapeResponse
{
    public required string ReleaseVersion { get; init; }
    public required int FetchedCount { get; init; }
    public required long PersistedCount { get; init; }
}
