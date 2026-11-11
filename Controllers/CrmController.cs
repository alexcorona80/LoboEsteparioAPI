using LoboEsteparioAPI.Models.Mongo;
using LoboEsteparioAPI.Models.Requests;
using LoboEsteparioAPI.Models.Responses;
using LoboEsteparioAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace LoboEsteparioAPI.Controllers;

[ApiController]
[Route("api/crm")]
public sealed class CrmController(
    IBusinessRepository businessRepository,
    IEmailService emailService,
    ISentEmailRepository sentEmailRepository,
    IEmailTemplateRepository templateRepository) : ControllerBase
{
    /// <summary>
    /// Lista los negocios de BusinessList que tienen al menos un correo registrado (destinatarios del CRM).
    /// Admite filtrado por categoría y reglas anti-spam (excluir por ronda y días de enfriamiento).
    /// </summary>
    [HttpGet("recipients")]
    public async Task<ActionResult<IReadOnlyList<BusinessDocument>>> Recipients(
        [FromQuery] int? limit,
        [FromQuery] string? category,
        [FromQuery] string? excludeRound,
        [FromQuery] int? cooldownDays,
        CancellationToken cancellationToken)
    {
        var effectiveLimit = Math.Clamp(limit ?? 500, 1, 2000);
        var businesses = await businessRepository.ListWithEmailsAsync(
            effectiveLimit,
            category,
            excludeRound,
            cooldownDays,
            cancellationToken);

        return Ok(businesses);
    }

    /// <summary>Lista las plantillas de correo guardadas en MongoDB (inicializa por defecto si está vacía).</summary>
    [HttpGet("templates")]
    public async Task<ActionResult<IReadOnlyList<EmailTemplateDocument>>> GetTemplates(CancellationToken cancellationToken)
    {
        var templates = await templateRepository.ListAsync(cancellationToken);
        return Ok(templates);
    }

    /// <summary>Crea o actualiza una plantilla de correo en MongoDB.</summary>
    [HttpPost("templates")]
    public async Task<ActionResult<EmailTemplateDocument>> SaveTemplate(
        [FromBody] EmailTemplateDocument template,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(template.Name))
        {
            return Problem(detail: "El nombre de la plantilla es obligatorio.", statusCode: StatusCodes.Status400BadRequest, title: "Invalid template");
        }

        var saved = await templateRepository.SaveAsync(template, cancellationToken);
        return Ok(saved);
    }

    /// <summary>Elimina una plantilla de correo de MongoDB por su Id.</summary>
    [HttpDelete("templates/{id}")]
    public async Task<ActionResult<object>> DeleteTemplate(string id, CancellationToken cancellationToken)
    {
        var deleted = await templateRepository.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            return NotFound(new { message = "Plantilla no encontrada" });
        }
        return Ok(new { success = true });
    }

    /// <summary>Envía un correo (o campaña) a los destinatarios indicados usando la configuración SMTP del body y registra el historial.</summary>
    [HttpPost("send")]
    [ProducesResponseType<EmailSendResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EmailSendResponse>> Send(SendEmailRequest request, CancellationToken cancellationToken)
    {
        if (request.Recipients.Count == 0 || request.Recipients.Any(r => string.IsNullOrWhiteSpace(r.ToEmail)))
        {
            return Problem(detail: "Se requiere al menos un destinatario con correo.", statusCode: StatusCodes.Status400BadRequest, title: "Invalid send request");
        }
        if (string.IsNullOrWhiteSpace(request.Smtp.Host) || string.IsNullOrWhiteSpace(request.Smtp.Username) || string.IsNullOrWhiteSpace(request.Smtp.Password))
        {
            return Problem(detail: "Host, usuario y contraseña SMTP son obligatorios.", statusCode: StatusCodes.Status400BadRequest, title: "Invalid smtp configuration");
        }

        var results = await emailService.SendAsync(request, cancellationToken);
        return Ok(new EmailSendResponse
        {
            SentCount = results.Count(r => r.Status == SentEmailStatus.Sent),
            FailedCount = results.Count(r => r.Status == SentEmailStatus.Failed),
            Results = results,
        });
    }

    /// <summary>Envía un correo de prueba a la dirección indicada para validar la configuración SMTP (no se persiste).</summary>
    [HttpPost("test")]
    [ProducesResponseType<EmailTestResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EmailTestResponse>> Test(TestEmailRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ToEmail))
        {
            return Problem(detail: "Indica el correo de prueba.", statusCode: StatusCodes.Status400BadRequest, title: "Invalid test request");
        }

        var result = await emailService.SendTestAsync(request.Smtp, request.ToEmail, cancellationToken);
        return Ok(result);
    }

    /// <summary>Historial de correos enviados por el CRM (colección SentEmails), del más reciente al más antiguo.</summary>
    [HttpGet("history")]
    public async Task<ActionResult<IReadOnlyList<SentEmailDocument>>> History([FromQuery] int? limit, CancellationToken cancellationToken)
    {
        var effectiveLimit = Math.Clamp(limit ?? 100, 1, 1000);
        var history = await sentEmailRepository.ListAsync(effectiveLimit, cancellationToken);
        return Ok(history);
    }

    /// <summary>Estadísticas del CRM: envíos, fallidos, último envío y alcance de BusinessList.</summary>
    [HttpGet("stats")]
    public async Task<ActionResult<EmailStatsResponse>> Stats(CancellationToken cancellationToken)
    {
        var sentStats = await sentEmailRepository.GetStatsAsync(cancellationToken);
        var businessesWithEmail = await businessRepository.CountWithEmailsAsync(cancellationToken);
        return Ok(new EmailStatsResponse
        {
            SentCount = sentStats.SentCount,
            FailedCount = sentStats.FailedCount,
            LastSentAtUtc = sentStats.LastSentAtUtc,
            OpenedCount = sentStats.OpenedCount,
            BusinessesWithEmailCount = businessesWithEmail,
        });
    }
}