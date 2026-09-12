using LoboEsteparioAPI.Models.Overture;
using LoboEsteparioAPI.Models.Requests;

namespace LoboEsteparioAPI.Services;

public interface IOverturePlacesService
{
    Task<IReadOnlyList<OverturePlace>> SearchAsync(PlaceSearchRequest request, CancellationToken cancellationToken = default);
}
