using LoboEsteparioAPI.Models.Overture;

namespace LoboEsteparioAPI.Models.Responses;

public sealed class PlaceSearchResponse
{
    public required string ReleaseVersion { get; init; }
    public required int Count { get; init; }
    public required IReadOnlyList<OverturePlace> Places { get; init; }
}
