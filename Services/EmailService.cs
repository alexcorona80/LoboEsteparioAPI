using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using LoboEsteparioAPI.Models.Mongo;
using LoboEsteparioAPI.Models.Requests;
using LoboEsteparioAPI.Models.Responses;
using LoboEsteparioAPI.Options;
using Microsoft.Extensions.Options;

namespace LoboEsteparioAPI.Services;

public sealed class EmailService(
    ISentEmailRepository sentEmailRepository,
    IBusinessRepository businessRepository,
    IOptions<EmailOptions> emailOptions) : IEmailService
{
    private const string PixelToken = "{TrackingPixel}";

    public async Task<IReadOnlyList<SentEmailDocument>> SendAsync(SendEmailRequest request, CancellationToken cancellationToken = default)
    {
        var results = new List<SentEmailDocument>(request.Recipients.Count);
        foreach (var recipient in request.Recipients)
        {
            var sentDoc = await SendSingleAsync(request.Smtp, recipient, request.Subject, request.Body, request.CampaignRound, cancellationToken);
            results.Add(sentDoc);

            if (sentDoc.Status == SentEmailStatus.Sent && !string.IsNullOrWhiteSpace(recipient.BusinessId))
            {
                await businessRepository.RegisterEmailSentAsync(recipient.BusinessId, request.CampaignRound, sentDoc.SentAtUtc, cancellationToken);
            }
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

    private async Task<SentEmailDocument> SendSingleAsync(
        SmtpConfig smtp,
        EmailRecipient recipient,
        string subject,
        string body,
        string? campaignRound,
        CancellationToken cancellationToken)
    {
        var options = emailOptions.Value;
        var businessName = recipient.BusinessName ?? string.Empty;
        var category = HumanizeCategory(recipient.Category);
        var location = recipient.Location ?? string.Empty;
        var senderName = !string.IsNullOrWhiteSpace(smtp.FromName) ? smtp.FromName : smtp.Username;
        var isHtml = EmailContent.LooksLikeHtml(body);
        var trackingId = Guid.NewGuid().ToString("N");

        var resolvedSubject = ApplyPlaceholders(subject, businessName, category, location, senderName, html: false, options.LogoUrl);
        var resolvedBody = ApplyPlaceholders(body, businessName, category, location, senderName, isHtml, options.LogoUrl);

        // Lo que se guarda en el historial: el cuerpo ya resuelto, sin el píxel.
        var storedBody = resolvedBody.Replace(PixelToken, string.Empty, StringComparison.OrdinalIgnoreCase);
        var htmlToSend = storedBody;
        if (isHtml && !string.IsNullOrWhiteSpace(options.PublicBaseUrl))
        {
            var pixelUrl = $"{options.PublicBaseUrl.TrimEnd('/')}/api/track/open/{trackingId}.gif";
            var pixelTag = $"<img src=\"{pixelUrl}\" width=\"1\" height=\"1\" alt=\"\" style=\"display:block;border:0;width:1px;height:1px;\" />";
            htmlToSend = resolvedBody.Contains(PixelToken, StringComparison.OrdinalIgnoreCase)
                ? resolvedBody.Replace(PixelToken, pixelTag, StringComparison.OrdinalIgnoreCase)
                : EmailContent.InjectPixel(resolvedBody, pixelTag);
        }

        var sentAt = DateTime.UtcNow;

        try
        {
            using var message = new MailMessage
            {
                From = new MailAddress(smtp.Username, smtp.FromName ?? string.Empty, Encoding.UTF8),
                Subject = resolvedSubject,
                SubjectEncoding = Encoding.UTF8,
                BodyEncoding = Encoding.UTF8,
                HeadersEncoding = Encoding.UTF8,
            };
            message.To.Add(recipient.ToEmail);
            message.Headers.Add("List-Unsubscribe", $"<mailto:{smtp.Username}?subject=BAJA>");

            if (isHtml)
            {
                // multipart/alternative: texto plano + HTML (los clientes muestran la última parte que soportan).
                message.Body = EmailContent.ToPlainText(storedBody);
                message.IsBodyHtml = false;
                message.BodyTransferEncoding = TransferEncoding.QuotedPrintable;

                var htmlView = AlternateView.CreateAlternateViewFromString(htmlToSend, Encoding.UTF8, MediaTypeNames.Text.Html);
                htmlView.TransferEncoding = TransferEncoding.QuotedPrintable;
                message.AlternateViews.Add(htmlView);
            }
            else
            {
                message.Body = resolvedBody;
                message.IsBodyHtml = false;
            }

            using var client = CreateClient(smtp);
            await client.SendMailAsync(message, cancellationToken);

            return new SentEmailDocument
            {
                BusinessId = recipient.BusinessId,
                BusinessName = businessName,
                ToEmail = recipient.ToEmail,
                Subject = resolvedSubject,
                Body = storedBody,
                SentAtUtc = sentAt,
                Status = SentEmailStatus.Sent,
                CampaignRound = campaignRound,
                TrackingId = trackingId,
                IsHtml = isHtml,
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
                Body = storedBody,
                SentAtUtc = sentAt,
                Status = SentEmailStatus.Failed,
                CampaignRound = campaignRound,
                Error = ex.Message,
                TrackingId = trackingId,
                IsHtml = isHtml,
            };
        }
    }

    /// <summary>
    /// Sustituye los marcadores. En HTML los valores del negocio se escapan; {EmpresaUrl} va codificado para usarse en enlaces
    /// (ej. mailto:...?subject=Cotización para {EmpresaUrl}) y {LogoUrl} sale de Email:LogoUrl.
    /// </summary>
    private static string ApplyPlaceholders(
        string template, string businessName, string category, string location, string senderName, bool html, string? logoUrl)
    {
        string Value(string text) => html ? WebUtility.HtmlEncode(text) : text;

        var result = template
            .Replace("{Empresa}", Value(businessName), StringComparison.OrdinalIgnoreCase)
            .Replace("{Categoria}", Value(category), StringComparison.OrdinalIgnoreCase)
            .Replace("{Ubicacion}", Value(location), StringComparison.OrdinalIgnoreCase)
            .Replace("{Remitente}", Value(senderName), StringComparison.OrdinalIgnoreCase)
            .Replace("{EmpresaUrl}", Uri.EscapeDataString(businessName), StringComparison.OrdinalIgnoreCase);

        if (html)
        {
            result = result.Replace("{LogoUrl}", WebUtility.HtmlEncode(logoUrl ?? string.Empty), StringComparison.OrdinalIgnoreCase);
        }

        return result;
    }

    /// <summary>"auto_dealer" → "Auto dealer" (las categorías de Overture vienen como etiquetas con guion bajo).</summary>
    private static string HumanizeCategory(string? category)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            return string.Empty;
        }

        var text = category.Replace('_', ' ').Trim();
        return char.ToUpperInvariant(text[0]) + text[1..];
    }

    private static SmtpClient CreateClient(SmtpConfig smtp) => new(smtp.Host, smtp.Port)
    {
        EnableSsl = smtp.EnableSsl,
        Credentials = new NetworkCredential(smtp.Username, smtp.Password),
        DeliveryMethod = SmtpDeliveryMethod.Network,
    };
}