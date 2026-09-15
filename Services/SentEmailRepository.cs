using LoboEsteparioAPI.Models.Mongo;
using LoboEsteparioAPI.Models.Responses;
using MongoDB.Driver;

namespace LoboEsteparioAPI.Services;

public sealed class SentEmailRepository(IMongoDatabase database) : ISentEmailRepository
{
    private const string CollectionName = "SentEmails";
    private readonly IMongoCollection<SentEmailDocument> _collection = database.GetCollection<SentEmailDocument>(CollectionName);

    public async Task SaveAsync(IEnumerable<SentEmailDocument> documents, CancellationToken cancellationToken = default)
    {
        var list = documents.ToList();
        if (list.Count == 0)
        {
            return;
        }

        await _collection.InsertManyAsync(list, cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<SentEmailDocument>> ListAsync(int limit, CancellationToken cancellationToken = default) =>
        await _collection.Find(FilterDefinition<SentEmailDocument>.Empty)
            .SortByDescending(d => d.SentAtUtc)
            .Limit(limit)
            .ToListAsync(cancellationToken);

    public async Task<SentEmailStatsResponse> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        var sentCount = await _collection.CountDocumentsAsync(
            Builders<SentEmailDocument>.Filter.Eq(d => d.Status, SentEmailStatus.Sent), cancellationToken: cancellationToken);
        var failedCount = await _collection.CountDocumentsAsync(
            Builders<SentEmailDocument>.Filter.Eq(d => d.Status, SentEmailStatus.Failed), cancellationToken: cancellationToken);

        DateTime? lastSentAtUtc = null;
        var lastSentAt = await _collection.Find(FilterDefinition<SentEmailDocument>.Empty)
            .SortByDescending(d => d.SentAtUtc)
            .Limit(1)
            .Project(d => d.SentAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        if (lastSentAt != default)
        {
            lastSentAtUtc = lastSentAt;
        }

        return new SentEmailStatsResponse
        {
            SentCount = sentCount,
            FailedCount = failedCount,
            LastSentAtUtc = lastSentAtUtc,
        };
    }
}