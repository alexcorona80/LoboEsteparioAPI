using LoboEsteparioAPI.Models.Requests;
using LoboEsteparioAPI.Models.Responses;
using LoboEsteparioAPI.Options;
using LoboEsteparioAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace LoboEsteparioAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class PlacesController(IOverturePlacesService placesService, IOptions<OvertureOptions> options) : ControllerBase
{
    /// <summary>
    /// Busca negocios de Overture Maps (tema "places") dentro de un bounding box, opcionalmente
    /// filtrados por categoría y nombre.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<PlaceSearchResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PlaceSearchResponse>> Search(
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
            return Ok(new PlaceSearchResponse
            {
                ReleaseVersion = options.Value.ReleaseVersion,
                Count = places.Count,
                Places = places,
            });
        }
        catch (ArgumentException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest, title: "Invalid search request");
        }
    }
}
