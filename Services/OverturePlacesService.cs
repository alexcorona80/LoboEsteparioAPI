using System.Data.Common;
using System.Text;
using DuckDB.NET.Data;
using LoboEsteparioAPI.Data;
using LoboEsteparioAPI.Models.Overture;
using LoboEsteparioAPI.Models.Requests;
using LoboEsteparioAPI.Options;
using Microsoft.Extensions.Options;

namespace LoboEsteparioAPI.Services;

/// <summary>
/// Consulta el tema "places" de Overture (negocios) directamente desde sus archivos Parquet
/// remotos usando DuckDB, filtrando por el struct bbox en vez de ST_Intersects sobre la
/// geometría completa, según la guía de rendimiento de Overture para búsquedas por bounding box.
/// </summary>
public sealed class OverturePlacesService(IDuckDbConnectionFactory connectionFactory, IOptions<OvertureOptions> options)
    : IOverturePlacesService
{
    private readonly OvertureOptions _options = options.Value;

    public async Task<IReadOnlyList<OverturePlace>> SearchAsync(PlaceSearchRequest request, CancellationToken cancellationToken = default)
    {
        ValidateBoundingBox(request);
        var limit = Math.Clamp(request.Limit ?? _options.DefaultResultLimit, 1, _options.MaxResultLimit);

        var sql = new StringBuilder($"""
            SELECT
                id,
                names,
                basic_category,
                taxonomy,
                confidence,
                ST_X(geometry) AS longitude,
                ST_Y(geometry) AS latitude,
                addresses,
                phones,
                websites,
                emails,
                socials,
                CAST(list_max(list_transform(sources, x -> x.update_time)) AS TIMESTAMP) AS update_time
            FROM read_parquet('{_options.BuildDatasetPath()}', filename=true, hive_partitioning=1)
            WHERE bbox.xmin <= ? AND bbox.xmax >= ?
              AND bbox.ymin <= ? AND bbox.ymax >= ?
            """);

        var parameters = new List<DuckDBParameter>
        {
            new(request.MaxLongitude),
            new(request.MinLongitude),
            new(request.MaxLatitude),
            new(request.MinLatitude),
        };

        if (OvertureTaxonomyCategories.TryGetValues(request.Category, out var taxonomyValues))
        {
            // Categorías de TI: no existen como basic_category, se filtra por la jerarquía de taxonomía.
            sql.Append($" AND list_has_any(taxonomy.hierarchy, [{string.Join(", ", taxonomyValues.Select(_ => "?"))}])");
            parameters.AddRange(taxonomyValues.Select(v => new DuckDBParameter(v)));
        }
        else if (!string.IsNullOrWhiteSpace(request.Category))
        {
            sql.Append(" AND lower(basic_category) = lower(?)");
            parameters.Add(new DuckDBParameter(request.Category));
        }

        if (!string.IsNullOrWhiteSpace(request.NameContains))
        {
            sql.Append(" AND lower(names.primary) LIKE lower(?)");
            parameters.Add(new DuckDBParameter($"%{request.NameContains}%"));
        }

        sql.Append(" ORDER BY confidence DESC NULLS LAST LIMIT ?");
        parameters.Add(new DuckDBParameter(limit));

        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql.ToString();
        command.Parameters.AddRange(parameters);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var results = new List<OverturePlace>();
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(MapRow(reader));
        }

        return results;
    }

    private static void ValidateBoundingBox(PlaceSearchRequest request)
    {
        if (request.MinLongitude >= request.MaxLongitude || request.MinLatitude >= request.MaxLatitude)
        {
            throw new ArgumentException("The bounding box minimums must be less than its maximums.");
        }

        if (request.MinLongitude < -180 || request.MaxLongitude > 180 || request.MinLatitude < -90 || request.MaxLatitude > 90)
        {
            throw new ArgumentException("The bounding box must fall within [-180, 180] longitude and [-90, 90] latitude.");
        }
    }

    private static OverturePlace MapRow(DbDataReader reader)
    {
        var names = GetStructDictionary(reader, 1);
        var taxonomy = GetStructDictionary(reader, 3);
        var addresses = GetStructList(reader, 7);
        var firstAddress = addresses.Count > 0 ? addresses[0] : null;

        return new OverturePlace
        {
            Id = reader.GetString(0),
            Name = GetStructString(names, "primary"),
            MainCategory = reader.IsDBNull(2) ? null : reader.GetString(2),
            DetailedCategory = GetStructString(taxonomy, "primary"),
            AlternateCategories = ExtractStringList(taxonomy?.GetValueOrDefault("alternates")),
            Confidence = reader.IsDBNull(4) ? null : reader.GetDouble(4),
            Longitude = reader.IsDBNull(5) ? null : reader.GetDouble(5),
            Latitude = reader.IsDBNull(6) ? null : reader.GetDouble(6),
            Address = GetStructString(firstAddress, "freeform"),
            Locality = GetStructString(firstAddress, "locality"),
            Region = GetStructString(firstAddress, "region"),
            Country = GetStructString(firstAddress, "country"),
            PostCode = GetStructString(firstAddress, "postcode"),
            Phones = reader.IsDBNull(8) ? [] : reader.GetFieldValue<List<string>>(8),
            Websites = reader.IsDBNull(9) ? [] : reader.GetFieldValue<List<string>>(9),
            Emails = reader.IsDBNull(10) ? [] : reader.GetFieldValue<List<string>>(10),
            Socials = reader.IsDBNull(11) ? [] : reader.GetFieldValue<List<string>>(11),
            UpdateTime = reader.IsDBNull(12) ? null : new DateTimeOffset(reader.GetDateTime(12), TimeSpan.Zero),
        };
    }

    private static Dictionary<string, object?>? GetStructDictionary(DbDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : (Dictionary<string, object?>)reader.GetValue(ordinal);

    private static List<Dictionary<string, object?>> GetStructList(DbDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal))
        {
            return [];
        }

        var raw = (System.Collections.IEnumerable)reader.GetValue(ordinal);
        return raw.OfType<Dictionary<string, object?>>().ToList();
    }

    private static string? GetStructString(Dictionary<string, object?>? dict, string key) =>
        dict is not null && dict.TryGetValue(key, out var value) && value is string s ? s : null;

    private static IReadOnlyList<string> ExtractStringList(object? value) => value switch
    {
        List<string> strings => strings,
        System.Collections.IEnumerable enumerable => enumerable.OfType<object>()
            .Select(x => x?.ToString())
            .Where(x => x is not null)
            .Select(x => x!)
            .ToList(),
        _ => [],
    };
}
