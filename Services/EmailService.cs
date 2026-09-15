using System.Net;
using System.Net.Mail;
using LoboEsteparioAPI.Models.Mongo;
using LoboEsteparioAPI.Models.Requests;
using LoboEsteparioAPI.Models.Responses;

namespace LoboEsteparioAPI.Services;

public sealed class EmailService(ISentEmailRepository sentEmailRepository) : IEmailService
{
    public async Task<IReadOnlyList<SentEmailDocument>> SendAsync(SendEmailRequest request, CancellationToken cancellationToken = default)
    {
        var results = new List<SentEmailDocument>(request.Recipients.Count);
        foreach (var recipient in request.Recipients)
        {
            results.Add(await SendSingleAsync(request.Smtp, recipient, request.Subject, request.Body, cancellationToken));
        }

        await sentEmailRepository.SaveAsync(results, cancellationToken);
        return results;
    }

    public async Task<EmailTestResponse> SendTestAsync(SmtpConfig smtp, string toEmail, CancellationToken cancellationToken = default)
    {
        try
        {
            using var message = new MailMessage
            {
                From = new MailAddress(smtp.Username, smtp.FromName ?? string.Empty),
                Subject = "Lobo Estepario · Prueba de configuración de correo",
                Body = "Prueba de envío desde el módulo CRM de Lobo Estepario.",
                IsBodyHtml = false,
            };
            message.To.Add(toEmail);

            using var client = CreateClient(smtp);
            await client.SendMailAsync(message, cancellationToken);

            return new EmailTestResponse { Success = true };
        }
        catch (Exception ex)
        {
            return new EmailTestResponse { Success = false, Error = ex.Message };
        }
    }

    private async Task<SentEmailDocument> SendSingleAsync(SmtpConfig smtp, EmailRecipient recipient, string subject, string body, CancellationToken cancellationToken)
    {
        var businessName = recipient.BusinessName ?? string.Empty;
        var resolvedSubject = subject.Replace("{Empresa}", businessName);
        var resolvedBody = body.Replace("{Empresa}", businessName);
        var sentAt = DateTime.UtcNow;

        try
        {
            using var message = new MailMessage
            {
                From = new MailAddress(smtp.Username, smtp.FromName ?? string.Empty),
                Subject = resolvedSubject,
                Body = resolvedBody,
                IsBodyHtml = false,
            };
            message.To.Add(recipient.ToEmail);

            using var client = CreateClient(smtp);
            await client.SendMailAsync(message, cancellationToken);

            return new SentEmailDocument
            {
                BusinessId = recipient.BusinessId,
                BusinessName = businessName,
                ToEmail = recipient.ToEmail,
                Subject = resolvedSubject,
                Body = resolvedBody,
                SentAtUtc = sentAt,
                Status = SentEmailStatus.Sent,
            };
        }
        catch (Exception ex)
        {
            return new SentEmailDocument
            {
                BusinessId = recipient.BusinessId,
                BusinessName = businessName,
                ToEmail = recipient.ToEmail,
                Subject = resolvedSubject,
                Body = resolvedBody,
                SentAtUtc = sentAt,
                Status = SentEmailStatus.Failed,
                Error = ex.Message,
            };
        }
    }

    private static SmtpClient CreateClient(SmtpConfig smtp) => new(smtp.Host, smtp.Port)
    {
        EnableSsl = smtp.EnableSsl,
        Credentials = new NetworkCredential(smtp.Username, smtp.Password),
        DeliveryMethod = SmtpDeliveryMethod.Network,
    };
}