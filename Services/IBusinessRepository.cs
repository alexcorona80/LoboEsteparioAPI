using LoboEsteparioAPI.Models.Mongo;
using LoboEsteparioAPI.Models.Responses;

namespace LoboEsteparioAPI.Services;

public interface IBusinessRepository
{
    /// <summary>Hace upsert de cada negocio por su id de Overture, para que volver a scrapear la misma zona actualice en vez de duplicar.</summary>
    Task<long> UpsertManyAsync(IEnumerable<BusinessDocument> businesses, CancellationToken cancellationToken = default);

    /// <summary>Lista los negocios de la colección, opcionalmente filtrados por categoría.</summary>
    Task<IReadOnlyList<BusinessDocument>> ListAsync(int limit, string? category = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista solo negocios con correo registrado, con filtros opcionales de categoría y reglas anti-spam (por ronda o días de enfriamiento).
    /// </summary>
    Task<IReadOnlyList<BusinessDocument>> ListWithEmailsAsync(
        int limit,
        string? category = null,
        string? excludeRound = null,
        int? cooldownDays = null,
        CancellationToken cancellationToken = default);

    /// <summary>Cuenta los negocios de la colección que tienen al menos un correo registrado.</summary>
    Task<long> CountWithEmailsAsync(CancellationToken cancellationToken = default);

    Task<BusinessStatsResponse> GetStatsAsync(CancellationToken cancellationToken = default);

    /// <summary>Obtiene la lista de categorías distintas almacenadas en la base de datos.</summary>
    Task<IReadOnlyList<string>> GetCategoriesAsync(CancellationToken cancellationToken = default);

    /// <summary>Registra un envío exitoso sobre el negocio (actualiza fecha de contacto, ronda y contador).</summary>
    Task RegisterEmailSentAsync(string businessId, string? campaignRound, DateTime sentAtUtc, CancellationToken cancellationToken = default);
}
