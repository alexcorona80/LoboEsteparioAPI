namespace LoboEsteparioAPI.Models.Responses;

/// <summary>Estadísticas agregadas del módulo CRM: historial de envíos + alcance de BusinessList.</summary>
public sealed class EmailStatsResponse
{
    public required long SentCount { get; init; }
    public required long FailedCount { get; init; }
    public required long BusinessesWithEmailCount { get; init; }
    public DateTime? LastSentAtUtc { get; init; }

    /// <summary>Correos con al menos una apertura registrada por el píxel.</summary>
    public long OpenedCount { get; init; }
}