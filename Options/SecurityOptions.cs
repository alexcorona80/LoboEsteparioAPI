namespace LoboEsteparioAPI.Options;

/// <summary>Claves de seguridad del servidor. Se definen por variables de entorno o user-secrets, nunca en el repositorio.</summary>
public sealed class SecurityOptions
{
    public const string SectionName = "Security";

    /// <summary>Clave AES-256 (32 bytes en Base64) con la que se cifran los secretos guardados en AIConfig.</summary>
    public string? SecretsKey { get; set; }

    /// <summary>Clave que deben enviar los clientes en la cabecera X-Api-Key para usar /api/aiconfig.</summary>
    public string? ApiKey { get; set; }
}
