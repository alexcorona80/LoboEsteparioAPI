using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace LoboEsteparioAPI.Models.Mongo;

/// <summary>Registro de un correo enviado por el módulo CRM (colección SentEmails).</summary>
public sealed class SentEmailDocument
{
    [BsonId]
    public ObjectId Id { get; init; } = ObjectId.GenerateNewId();

    public string? BusinessId { get; init; }
    public string? BusinessName { get; init; }
    public required string ToEmail { get; init; }
    public required string Subject { get; init; }
    public required string Body { get; init; }
    public required DateTime SentAtUtc { get; init; }
    public required SentEmailStatus Status { get; init; }
    public string? CampaignRound { get; init; }
    public string? Error { get; init; }

    /// <summary>Identificador único del envío; viaja en el píxel de apertura del correo HTML.</summary>
    public string? TrackingId { get; init; }

    /// <summary>True si el correo se envió como HTML (multipart/alternative).</summary>
    public bool IsHtml { get; init; }

    /// <summary>Veces que se cargó el píxel de apertura (puede incluir precargas de Apple Mail/Gmail).</summary>
    public int OpenCount { get; init; }

    public DateTime? FirstOpenedAtUtc { get; init; }

    public DateTime? LastOpenedAtUtc { get; init; }
}

public enum SentEmailStatus
{
    Sent,
    Failed
}