namespace LoboEsteparioAPI.Models.Requests;

/// <summary>Petición para validar la configuración SMTP enviando un correo de prueba a una dirección.</summary>
public sealed class TestEmailRequest
{
    public required SmtpConfig Smtp { get; init; }
    public required string ToEmail { get; init; }
}