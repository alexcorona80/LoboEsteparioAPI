using MongoDB.Bson.Serialization.Attributes;

namespace LoboEsteparioAPI.Models.Mongo;

/// <summary>
/// Configuración persistida en la colección AIConfig: proveedor de IA, cuenta de correo, prompts, etc.
/// Un documento por clave (_id), por ejemplo "ai-provider", "smtp" o "prompt-email-copy".
/// </summary>
public sealed class AIConfigDocument
{
    /// <summary>Clave estable del documento (minúsculas, números, punto, guion).</summary>
    [BsonId]
    public required string Id { get; set; }

    /// <summary>Categoría: "prompt", "ai_provider", "smtp"…</summary>
    public required string Type { get; set; }

    public string? Name { get; set; }

    /// <summary>Valores no secretos (host, puerto, modelo, texto del prompt…).</summary>
    public Dictionary<string, string?> Values { get; set; } = new();

    /// <summary>Valores secretos (contraseñas, API keys) cifrados con AES-GCM en Base64. Nunca se devuelven por la API.</summary>
    public Dictionary<string, string> Secrets { get; set; } = new();

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }
}
