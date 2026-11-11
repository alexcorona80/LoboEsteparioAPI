namespace LoboEsteparioAPI.Models.Responses;

/// <summary>Estadísticas de la colección SentEmails (historial del CRM).</summary>
public sealed class SentEmailStatsResponse
{
    public required long SentCount { get; init; }
    public required long FailedCount { get; init; }
    public DateTime? LastSentAtUtc { get; init; }

    /// <summary>Correos con al menos una apertura registrada por el píxel.</summary>
    public long OpenedCount { get; init; }
}