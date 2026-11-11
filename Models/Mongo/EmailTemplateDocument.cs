using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace LoboEsteparioAPI.Models.Mongo;

/// <summary>
/// Plantilla de correo persistida en MongoDB (colección EmailTemplates).
/// Permite redactar campañas en el CRM con marcadores dinámicos.
/// </summary>
public sealed class EmailTemplateDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public required string Name { get; set; }

    public required string Subject { get; set; }

    public required string Body { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }

    public bool IsDefault { get; set; }
}
