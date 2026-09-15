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
    public string? Error { get; init; }
}

public enum SentEmailStatus
{
    Sent,
    Failed
}