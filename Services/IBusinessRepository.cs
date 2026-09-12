using LoboEsteparioAPI.Models.Mongo;
using LoboEsteparioAPI.Models.Responses;

namespace LoboEsteparioAPI.Services;

public interface IBusinessRepository
{
    /// <summary>Hace upsert de cada negocio por su id de Overture, para que volver a scrapear la misma zona actualice en vez de duplicar.</summary>
    Task<long> UpsertManyAsync(IEnumerable<BusinessDocument> businesses, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BusinessDocument>> ListAsync(int limit, CancellationToken cancellationToken = default);

    Task<BusinessStatsResponse> GetStatsAsync(CancellationToken cancellationToken = default);
}
