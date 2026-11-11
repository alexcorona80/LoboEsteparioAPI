using LoboEsteparioAPI.Models.Mongo;
using LoboEsteparioAPI.Models.Requests;

namespace LoboEsteparioAPI.Services;

public interface IAIConfigRepository
{
    Task<IReadOnlyList<AIConfigDocument>> ListAsync(CancellationToken cancellationToken = default);

    Task<AIConfigDocument?> GetAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>Crea o actualiza el documento. Cifra los secretos recibidos; lanza InvalidOperationException si falta Security:SecretsKey.</summary>
    Task<AIConfigDocument> UpsertAsync(string id, AIConfigUpsertRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>Descifra un secreto (uso interno del servidor, por ejemplo el agente de IA). Devuelve null si no existe.</summary>
    Task<string?> GetSecretAsync(string id, string key, CancellationToken cancellationToken = default);

    /// <summary>Crea la colección AIConfig con sus documentos iniciales si faltan (no sobrescribe los existentes).</summary>
    Task EnsureSeedAsync(CancellationToken cancellationToken = default);
}
