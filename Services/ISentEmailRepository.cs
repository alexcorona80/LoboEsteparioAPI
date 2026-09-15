using LoboEsteparioAPI.Models.Mongo;
using LoboEsteparioAPI.Models.Responses;

namespace LoboEsteparioAPI.Services;

public interface ISentEmailRepository
{
    Task SaveAsync(IEnumerable<SentEmailDocument> documents, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SentEmailDocument>> ListAsync(int limit, CancellationToken cancellationToken = default);

    Task<SentEmailStatsResponse> GetStatsAsync(CancellationToken cancellationToken = default);
}