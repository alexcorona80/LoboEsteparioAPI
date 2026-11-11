using System.Text.RegularExpressions;
using LoboEsteparioAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace LoboEsteparioAPI.Controllers;

/// <summary>Píxel de apertura de los correos HTML. Es público a propósito: lo solicitan los clientes de correo de los destinatarios.</summary>
[ApiController]
[Route("api/track")]
public sealed class TrackingController(ISentEmailRepository sentEmailRepository, ILogger<TrackingController> logger) : ControllerBase
{
    private static readonly Regex TrackingIdPattern = new("^[0-9a-f]{32}$", RegexOptions.Compiled);

    // GIF transparente de 1x1.
    private static readonly byte[] TransparentGif =
        Convert.FromBase64String("R0lGODlhAQABAIAAAAAAAP///yH5BAEAAAAALAAAAAABAAEAAAIBRAA7");

    /// <summary>Registra una apertura del correo y devuelve siempre el GIF (aunque el id no exista) para no revelar información.</summary>
    [HttpGet("open/{trackingId}.gif")]
    public async Task<IActionResult> Open(string trackingId)
    {
        if (TrackingIdPattern.IsMatch(trackingId))
        {
            try
            {
                // CancellationToken.None: algunos clientes cierran la conexión antes de terminar de leer la imagen.
                await sentEmailRepository.RegisterOpenAsync(trackingId, CancellationToken.None);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "No se pudo registrar la apertura {TrackingId}", trackingId);
            }
        }

        Response.Headers.CacheControl = "no-store, no-cache, must-revalidate, max-age=0";
        Response.Headers.Pragma = "no-cache";
        return File(TransparentGif, "image/gif");
    }
}
