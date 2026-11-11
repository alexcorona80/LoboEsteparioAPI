namespace LoboEsteparioAPI.Models.Requests;

/// <summary>Crea o actualiza un documento de AIConfig.</summary>
public sealed class AIConfigUpsertRequest
{
    public string? Type { get; set; }

    public string? Name { get; set; }

    /// <summary>Si viene, reemplaza por completo los valores no secretos.</summary>
    public Dictionary<string, string?>? Values { get; set; }

    /// <summary>Secretos en claro a guardar (se cifran). Las claves ausentes o vacías conservan su valor actual.</summary>
    public Dictionary<string, string?>? Secrets { get; set; }

    /// <summary>Nombres de secretos que se deben borrar.</summary>
    public List<string>? ClearSecrets { get; set; }
}
