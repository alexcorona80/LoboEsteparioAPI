using LoboEsteparioAPI.Models.Mongo;
using LoboEsteparioAPI.Models.Responses;

namespace LoboEsteparioAPI.Services;

public interface ISentEmailRepository
{
    Task SaveAsync(IEnumerable<SentEmailDocument> documents, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SentEmailDocument>> ListAsync(int limit, CancellationToken cancellationToken = default);

    Task<SentEmailStatsResponse> GetStatsAsync(CancellationToken cancellationToken = default);

    /// <summary>Suma una apertura al envío con ese TrackingId. Devuelve false si no existe.</summary>
    Task<bool> RegisterOpenAsync(string trackingId, CancellationToken cancellationToken = default);

    /// <summary>Crea los índices necesarios (TrackingId).</summary>
    Task EnsureIndexesAsync(CancellationToken cancellationToken = default);
}