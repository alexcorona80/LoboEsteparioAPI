using LoboEsteparioAPI.Models.Mongo;
using LoboEsteparioAPI.Models.Responses;

namespace LoboEsteparioAPI.Services;

public interface IBusinessRepository
{
    /// <summary>Hace upsert de cada negocio por su id de Overture, para que volver a scrapear la misma zona actualice en vez de duplicar.</summary>
    Task<long> UpsertManyAsync(IEnumerable<BusinessDocument> businesses, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BusinessDocument>> ListAsync(int limit, CancellationToken cancellationToken = default);

    /// <summary>Lista solo negocios cuyo array Emails tenga al menos un elemento registrado.</summary>
    Task<IReadOnlyList<BusinessDocument>> ListWithEmailsAsync(int limit, CancellationToken cancellationToken = default);

    /// <summary>Cuenta los negocios de la colección que tienen al menos un correo registrado.</summary>
    Task<long> CountWithEmailsAsync(CancellationToken cancellationToken = default);

    Task<BusinessStatsResponse> GetStatsAsync(CancellationToken cancellationToken = default);
}
