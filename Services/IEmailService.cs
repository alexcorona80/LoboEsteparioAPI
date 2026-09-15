using LoboEsteparioAPI.Models.Mongo;
using LoboEsteparioAPI.Models.Requests;
using LoboEsteparioAPI.Models.Responses;

namespace LoboEsteparioAPI.Services;

public interface IEmailService
{
    /// <summary>Envía el correo a cada destinatario con la configuración SMTP dada y registra cada resultado en la colección SentEmails.</summary>
    Task<IReadOnlyList<SentEmailDocument>> SendAsync(SendEmailRequest request, CancellationToken cancellationToken = default);

    /// <summary>Envía un correo de prueba a una dirección sin persistirlo en el historial.</summary>
    Task<EmailTestResponse> SendTestAsync(SmtpConfig smtp, string toEmail, CancellationToken cancellationToken = default);
}