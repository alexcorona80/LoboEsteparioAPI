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

    public async Task<IReadOnlyList<BusinessDocument>> ListAsync(int limit, string? category = null, CancellationToken cancellationToken = default)
    {
        var filter = FilterDefinition<BusinessDocument>.Empty;
        if (!string.IsNullOrWhiteSpace(category))
        {
            var cat = category.Trim();
            filter = Builders<BusinessDocument>.Filter.Or(
                Builders<BusinessDocument>.Filter.Eq(b => b.CategoryTag, cat),
                Builders<BusinessDocument>.Filter.Eq(b => b.MainCategory, cat));
        }

        return await _collection.Find(filter)
            .SortByDescending(b => b.ScrapedAtUtc)
            .Limit(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<BusinessDocument>> ListWithEmailsAsync(
        int limit,
        string? category = null,
        string? excludeRound = null,
        int? cooldownDays = null,
        CancellationToken cancellationToken = default)
    {
        var filters = new List<FilterDefinition<BusinessDocument>> { HasEmailFilter };

        if (!string.IsNullOrWhiteSpace(category))
        {
            var cat = category.Trim();
            filters.Add(Builders<BusinessDocument>.Filter.Or(
                Builders<BusinessDocument>.Filter.Eq(b => b.CategoryTag, cat),
                Builders<BusinessDocument>.Filter.Eq(b => b.MainCategory, cat)));
        }

        if (!string.IsNullOrWhiteSpace(excludeRound))
        {
            var round = excludeRound.Trim();
            filters.Add(Builders<BusinessDocument>.Filter.Ne(b => b.LastCampaignRound, round));
        }

        if (cooldownDays.HasValue && cooldownDays.Value > 0)
        {
            var cutoff = DateTime.UtcNow.AddDays(-cooldownDays.Value);
            // Elegible si nunca fue contactado o fue contactado antes del corte
            filters.Add(Builders<BusinessDocument>.Filter.Or(
                Builders<BusinessDocument>.Filter.Eq(b => b.LastContactedAtUtc, null),
                Builders<BusinessDocument>.Filter.Lt(b => b.LastContactedAtUtc, cutoff)));
        }

        var combinedFilter = Builders<BusinessDocument>.Filter.And(filters);

        return await _collection.Find(combinedFilter)
            .SortBy(b => b.LastContactedAtUtc) // Los menos recientemente contactados primero
            .Limit(limit)
            .ToListAsync(cancellationToken);
    }

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

    public async Task<IReadOnlyList<string>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        var catTags = await _collection.Distinct(b => b.CategoryTag, FilterDefinition<BusinessDocument>.Empty, cancellationToken: cancellationToken)
            .ToListAsync(cancellationToken);
        var mainCats = await _collection.Distinct(b => b.MainCategory, FilterDefinition<BusinessDocument>.Empty, cancellationToken: cancellationToken)
            .ToListAsync(cancellationToken);

        return catTags.Concat(mainCats)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(c => c)
            .ToList();
    }

    public async Task RegisterEmailSentAsync(string businessId, string? campaignRound, DateTime sentAtUtc, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(businessId))
        {
            return;
        }

        var update = Builders<BusinessDocument>.Update
            .Set(b => b.LastContactedAtUtc, sentAtUtc)
            .Inc(b => b.ContactCount, 1);

        if (!string.IsNullOrWhiteSpace(campaignRound))
        {
            update = update.Set(b => b.LastCampaignRound, campaignRound.Trim());
        }

        await _collection.UpdateOneAsync(
            b => b.Id == businessId,
            update,
            cancellationToken: cancellationToken);
    }
}
