using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using LoboEsteparioAPI.Models.Mongo;
using LoboEsteparioAPI.Models.Requests;
using LoboEsteparioAPI.Models.Responses;
using LoboEsteparioAPI.Options;
using LoboEsteparioAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace LoboEsteparioAPI.Controllers;

/// <summary>
/// Configuración guardada en la colección AIConfig (proveedor de IA, correo, prompts).
/// Requiere la cabecera X-Api-Key igual a Security:ApiKey; si el servidor no la tiene definida, el recurso queda deshabilitado.
/// Los secretos se cifran al guardarlos y nunca se devuelven.
/// </summary>
[ApiController]
[Route("api/aiconfig")]
public sealed class AIConfigController(
    IAIConfigRepository repository,
    ISecretProtector protector,
    IOptions<SecurityOptions> security) : ControllerBase
{
    private static readonly Regex IdPattern = new("^[a-z0-9][a-z0-9._-]{0,63}$", RegexOptions.Compiled);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AIConfigResponse>>> List(CancellationToken cancellationToken)
    {
        if (Authorize() is { } denied)
        {
            return denied;
        }

        var documents = await repository.ListAsync(cancellationToken);
        return Ok(documents.Select(ToResponse).ToList());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AIConfigResponse>> Get(string id, CancellationToken cancellationToken)
    {
        if (Authorize() is { } denied)
        {
            return denied;
        }

        var document = await repository.GetAsync(id, cancellationToken);
        if (document is null)
        {
            return NotFound(new { message = "Configuración no encontrada" });
        }

        return Ok(ToResponse(document));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<AIConfigResponse>> Put(string id, AIConfigUpsertRequest request, CancellationToken cancellationToken)
    {
        if (Authorize() is { } denied)
        {
            return denied;
        }

        if (!IdPattern.IsMatch(id))
        {
            return Problem(detail: "El id solo admite minúsculas, números, punto y guion (máx. 64).", statusCode: StatusCodes.Status400BadRequest, title: "Invalid id");
        }

        var hasSecrets = request.Secrets?.Values.Any(v => !string.IsNullOrEmpty(v)) == true;
        if (hasSecrets && !protector.IsConfigured)
        {
            return Problem(
                detail: "El servidor no tiene Security:SecretsKey (32 bytes en Base64); no se guardan secretos sin cifrar.",
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Secrets key missing");
        }

        var saved = await repository.UpsertAsync(id, request, cancellationToken);
        return Ok(ToResponse(saved));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<object>> Delete(string id, CancellationToken cancellationToken)
    {
        if (Authorize() is { } denied)
        {
            return denied;
        }

        var deleted = await repository.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            return NotFound(new { message = "Configuración no encontrada" });
        }

        return Ok(new { success = true });
    }

    /// <summary>Devuelve null si la petición está autorizada; en otro caso, la respuesta de error.</summary>
    private ActionResult? Authorize()
    {
        var expected = security.Value.ApiKey;
        if (string.IsNullOrWhiteSpace(expected))
        {
            return Problem(
                detail: "Define Security:ApiKey en el servidor para habilitar /api/aiconfig.",
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "AIConfig disabled");
        }

        var provided = Request.Headers["X-Api-Key"].ToString();
        var ok = CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(provided), Encoding.UTF8.GetBytes(expected));
        return ok
            ? null
            : Problem(detail: "Cabecera X-Api-Key ausente o incorrecta.", statusCode: StatusCodes.Status401Unauthorized, title: "Unauthorized");
    }

    private static AIConfigResponse ToResponse(AIConfigDocument document) => new()
    {
        Id = document.Id,
        Type = document.Type,
        Name = document.Name,
        Values = document.Values,
        SecretsSet = document.Secrets.Keys.OrderBy(k => k).ToList(),
        CreatedAtUtc = document.CreatedAtUtc,
        UpdatedAtUtc = document.UpdatedAtUtc,
    };
}
