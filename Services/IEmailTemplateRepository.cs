using LoboEsteparioAPI.Models.Mongo;

namespace LoboEsteparioAPI.Services;

public interface IEmailTemplateRepository
{
    Task<IReadOnlyList<EmailTemplateDocument>> ListAsync(CancellationToken cancellationToken = default);
    Task<EmailTemplateDocument?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<EmailTemplateDocument> SaveAsync(EmailTemplateDocument template, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
    Task SeedDefaultsIfEmptyAsync(CancellationToken cancellationToken = default);

    /// <summary>Inserta la plantilla si no existe otra con el mismo nombre. Devuelve true si la creó.</summary>
    Task<bool> EnsureAsync(EmailTemplateDocument template, CancellationToken cancellationToken = default);
}
