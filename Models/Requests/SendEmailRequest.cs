namespace LoboEsteparioAPI.Models.Requests;

/// <summary>Petición de envío de correo(s) a destinatarios de la colección BusinessList.</summary>
public sealed class SendEmailRequest
{
    /// <summary>Configuración SMTP de la cuenta remitente. Viaja con cada petición, no se guarda en la API.</summary>
    public required SmtpConfig Smtp { get; init; }

    /// <summary>Destinatarios. Pueden llevar el id de negocio de BusinessList para trazabilidad.</summary>
    public required IReadOnlyList<EmailRecipient> Recipients { get; init; }

    /// <summary>Asunto del correo. Acepta placeholders como {Empresa}, {Categoria}, {Ubicacion}.</summary>
    public required string Subject { get; init; }

    /// <summary>Cuerpo del correo (texto plano). Acepta placeholders como {Empresa}, {Categoria}, {Ubicacion}, {Remitente}.</summary>
    public required string Body { get; init; }

    /// <summary>Identificador o nombre de la ronda de campaña actual (ej. 'Ronda 1').</summary>
    public string? CampaignRound { get; init; }
}

/// <summary>Parámetros SMTP de la cuenta remitente, configurables desde el formulario de la app de escritorio.</summary>
public sealed class SmtpConfig
{
    public required string Host { get; init; }
    public required int Port { get; init; }
    public required string Username { get; init; }
    public required string Password { get; init; }
    public string? FromName { get; init; }
    public required bool EnableSsl { get; init; }
}

/// <summary>Un destinatario con el id y nombre de su negocio (para trazabilidad) y el correo a usar.</summary>
public sealed class EmailRecipient
{
    public string? BusinessId { get; init; }
    public string? BusinessName { get; init; }
    public string? Category { get; init; }
    public string? Location { get; init; }
    public required string ToEmail { get; init; }
}