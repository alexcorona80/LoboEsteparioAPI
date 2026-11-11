using LoboEsteparioAPI.Models.Mongo;
using LoboEsteparioAPI.Models.Requests;
using LoboEsteparioAPI.Models.Responses;
using LoboEsteparioAPI.Options;
using LoboEsteparioAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace LoboEsteparioAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class BusinessController(
    IOverturePlacesService placesService,
    IBusinessRepository businessRepository,
    IOptions<OvertureOptions> overtureOptions) : ControllerBase
{
    /// <summary>Lista los negocios ya persistidos en la colección BusinessList, con filtro opcional por categoría.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BusinessDocument>>> List(
        [FromQuery] int? limit,
        [FromQuery] string? category,
        CancellationToken cancellationToken)
    {
        var effectiveLimit = Math.Clamp(limit ?? 100, 1, 1000);
        var businesses = await businessRepository.ListAsync(effectiveLimit, category, cancellationToken);
        return Ok(businesses);
    }

    /// <summary>Lista todas las categorías distintas registradas en la colección de negocios.</summary>
    [HttpGet("categories")]
    public async Task<ActionResult<IReadOnlyList<string>>> GetCategories(CancellationToken cancellationToken)
    {
        var categories = await businessRepository.GetCategoriesAsync(cancellationToken);
        return Ok(categories);
    }

    /// <summary>Estadísticas agregadas de la colección (total, con contacto, fuentes activas, última ejecución).</summary>
    [HttpGet("stats")]
    public async Task<ActionResult<BusinessStatsResponse>> Stats(CancellationToken cancellationToken)
    {
        var stats = await businessRepository.GetStatsAsync(cancellationToken);
        return Ok(stats);
    }

    /// <summary>
    /// Scrapea negocios de Overture para el bounding box/filtros dados y hace upsert de ellos
    /// en la colección MongoDB Business/BusinessList asociando la categoría elegida.
    /// </summary>
    [HttpPost("scrape")]
    [ProducesResponseType<ScrapeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ScrapeResponse>> Scrape(
        [FromQuery] double minLongitude,
        [FromQuery] double minLatitude,
        [FromQuery] double maxLongitude,
        [FromQuery] double maxLatitude,
        [FromQuery] string? category,
        [FromQuery] string? nameContains,
        [FromQuery] int? limit,
        CancellationToken cancellationToken)
    {
        var request = PlaceSearchRequest.FromQuery(minLongitude, minLatitude, maxLongitude, maxLatitude, category, nameContains, limit);

        try
        {
            var places = await placesService.SearchAsync(request, cancellationToken);

            var scrapedAt = DateTimeOffset.UtcNow;
            var releaseVersion = overtureOptions.Value.ReleaseVersion;
            var documents = places.Select(place => BusinessDocument.FromOverturePlace(place, releaseVersion, scrapedAt, category));

            var persistedCount = await businessRepository.UpsertManyAsync(documents, cancellationToken);

            return Ok(new ScrapeResponse
            {
                ReleaseVersion = releaseVersion,
                FetchedCount = places.Count,
                PersistedCount = persistedCount,
            });
        }
        catch (ArgumentException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest, title: "Invalid scrape request");
        }
    }
}
