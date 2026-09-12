using LoboEsteparioAPI.Models.Overture;
using MongoDB.Bson.Serialization.Attributes;

namespace LoboEsteparioAPI.Models.Mongo;

/// <summary>Un negocio persistido en la colección BuisnessList, obtenido de un place de Overture.</summary>
public sealed class BusinessDocument
{
    /// <summary>El id GERS de Overture, reutilizado como _id de Mongo para que volver a scrapear actualice en vez de duplicar.</summary>
    [BsonId]
    public required string Id { get; init; }

    public string? Name { get; init; }
    public string? MainCategory { get; init; }
    public string? DetailedCategory { get; init; }
    public IReadOnlyList<string> AlternateCategories { get; init; } = [];
    public double? Confidence { get; init; }
    public double? Longitude { get; init; }
    public double? Latitude { get; init; }
    public string? Address { get; init; }
    public string? Locality { get; init; }
    public string? Region { get; init; }
    public string? Country { get; init; }
    public string? PostCode { get; init; }
    public IReadOnlyList<string> Phones { get; init; } = [];
    public IReadOnlyList<string> Websites { get; init; } = [];
    public IReadOnlyList<string> Emails { get; init; } = [];
    public IReadOnlyList<string> Socials { get; init; } = [];
    public DateTime? OvertureUpdateTimeUtc { get; init; }
    public required string OvertureReleaseVersion { get; init; }
    public required DateTime ScrapedAtUtc { get; init; }

    public static BusinessDocument FromOverturePlace(OverturePlace place, string releaseVersion, DateTimeOffset scrapedAt) => new()
    {
        Id = place.Id,
        Name = place.Name,
        MainCategory = place.MainCategory,
        DetailedCategory = place.DetailedCategory,
        AlternateCategories = place.AlternateCategories,
        Confidence = place.Confidence,
        Longitude = place.Longitude,
        Latitude = place.Latitude,
        Address = place.Address,
        Locality = place.Locality,
        Region = place.Region,
        Country = place.Country,
        PostCode = place.PostCode,
        Phones = place.Phones,
        Websites = place.Websites,
        Emails = place.Emails,
        Socials = place.Socials,
        OvertureUpdateTimeUtc = place.UpdateTime?.UtcDateTime,
        OvertureReleaseVersion = releaseVersion,
        ScrapedAtUtc = scrapedAt.UtcDateTime,
    };
}
