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

        var openedCount = await _collection.CountDocumentsAsync(
            Builders<SentEmailDocument>.Filter.Gt(d => d.OpenCount, 0), cancellationToken: cancellationToken);

        return new SentEmailStatsResponse
        {
            SentCount = sentCount,
            FailedCount = failedCount,
            LastSentAtUtc = lastSentAtUtc,
            OpenedCount = openedCount,
        };
    }

    public async Task<bool> RegisterOpenAsync(string trackingId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var byTrackingId = Builders<SentEmailDocument>.Filter.Eq(d => d.TrackingId, trackingId);

        var result = await _collection.UpdateOneAsync(
            byTrackingId,
            Builders<SentEmailDocument>.Update.Inc(d => d.OpenCount, 1).Set(d => d.LastOpenedAtUtc, now),
            cancellationToken: cancellationToken);
        if (result.MatchedCount == 0)
        {
            return false;
        }

        // Primera apertura: solo si aún no hay fecha (el documento guarda null al insertarse).
        await _collection.UpdateOneAsync(
            Builders<SentEmailDocument>.Filter.And(byTrackingId, Builders<SentEmailDocument>.Filter.Eq(d => d.FirstOpenedAtUtc, (DateTime?)null)),
            Builders<SentEmailDocument>.Update.Set(d => d.FirstOpenedAtUtc, now),
            cancellationToken: cancellationToken);
        return true;
    }

    public async Task EnsureIndexesAsync(CancellationToken cancellationToken = default)
    {
        var index = new CreateIndexModel<SentEmailDocument>(
            Builders<SentEmailDocument>.IndexKeys.Ascending(d => d.TrackingId),
            new CreateIndexOptions { Name = "ix_trackingid", Sparse = true });
        await _collection.Indexes.CreateOneAsync(index, cancellationToken: cancellationToken);
    }
}