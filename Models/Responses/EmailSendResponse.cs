using LoboEsteparioAPI.Models.Mongo;

namespace LoboEsteparioAPI.Models.Responses;

/// <summary>Resultado de un envío de correo: cuántos salieron bien, cuántos fallaron y el detalle por destinatario.</summary>
public sealed class EmailSendResponse
{
    public required int SentCount { get; init; }
    public required int FailedCount { get; init; }
    public required IReadOnlyList<SentEmailDocument> Results { get; init; }
}