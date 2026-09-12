namespace LoboEsteparioAPI.Models.Responses;

/// <summary>Estadísticas agregadas de la colección BusinessList, para el panel de la app de escritorio.</summary>
public sealed class BusinessStatsResponse
{
    public required long TotalCount { get; init; }
    public required long WithContactCount { get; init; }
    public required int ActiveSourceCount { get; init; }
    public DateTime? LastScrapedAtUtc { get; init; }
}
