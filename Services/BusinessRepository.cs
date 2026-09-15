using LoboEsteparioAPI.Models.Mongo;
using LoboEsteparioAPI.Models.Responses;
using LoboEsteparioAPI.Options;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace LoboEsteparioAPI.Services;

public sealed class BusinessRepository : IBusinessRepository
{
    private readonly IMongoCollection<BusinessDocument> _collection;

    /// <summary>Filtro para documentos cuyo array Emails exista y tenga al menos un elemento.</summary>
    private static readonly FilterDefinition<BusinessDocument> HasEmailFilter = Builders<BusinessDocument>.Filter.And(
        Builders<BusinessDocument>.Filter.Exists(b => b.Emails),
        Builders<BusinessDocument>.Filter.Not(Builders<BusinessDocument>.Filter.Size(b => b.Emails, 0)));

    public BusinessRepository(IMongoDatabase database, IOptions<MongoOptions> options)
    {
        _collection = database.GetCollection<BusinessDocument>(options.Value.CollectionName);
    }

    public async Task<long> UpsertManyAsync(IEnumerable<BusinessDocument> businesses, CancellationToken cancellationToken = default)
    {
        var models = businesses
            .Select(business => new ReplaceOneModel<BusinessDocument>(
                Builders<BusinessDocument>.Filter.Eq(b => b.Id, business.Id), business)
            { IsUpsert = true })
            .ToList();

        if (models.Count == 0)
        {
            return 0;
        }

        var result = await _collection.BulkWriteAsync(models, cancellationToken: cancellationToken);
        return result.Upserts.Count + result.ModifiedCount;
    }

    public async Task<IReadOnlyList<BusinessDocument>> ListAsync(int limit, CancellationToken cancellationToken = default) =>
        await _collection.Find(FilterDefinition<BusinessDocument>.Empty)
            .Limit(limit)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<BusinessDocument>> ListWithEmailsAsync(int limit, CancellationToken cancellationToken = default) =>
        await _collection.Find(HasEmailFilter)
            .Limit(limit)
            .ToListAsync(cancellationToken);

    public async Task<long> CountWithEmailsAsync(CancellationToken cancellationToken = default) =>
        await _collection.CountDocumentsAsync(HasEmailFilter, cancellationToken: cancellationToken);

    public async Task<BusinessStatsResponse> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        var totalCount = await _collection.CountDocumentsAsync(FilterDefinition<BusinessDocument>.Empty, cancellationToken: cancellationToken);

        var contactFields = await _collection.Find(FilterDefinition<BusinessDocument>.Empty)
            .Project(b => new { b.Emails, b.Phones, b.Websites })
            .ToListAsync(cancellationToken);
        var withContactCount = contactFields.Count(b =>
            (b.Emails?.Count ?? 0) > 0 || (b.Phones?.Count ?? 0) > 0 || (b.Websites?.Count ?? 0) > 0);

        var releaseVersions = await _collection.Distinct(b => b.OvertureReleaseVersion, FilterDefinition<BusinessDocument>.Empty, cancellationToken: cancellationToken)
            .ToListAsync(cancellationToken);

        DateTime? lastScrapedAtUtc = null;
        if (totalCount > 0)
        {
            lastScrapedAtUtc = await _collection.Find(FilterDefinition<BusinessDocument>.Empty)
                .SortByDescending(b => b.ScrapedAtUtc)
                .Limit(1)
                .Project(b => b.ScrapedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return new BusinessStatsResponse
        {
            TotalCount = totalCount,
            WithContactCount = withContactCount,
            ActiveSourceCount = releaseVersions.Count,
            LastScrapedAtUtc = lastScrapedAtUtc,
        };
    }
}
