namespace LoboEsteparioAPI.Services;

/// <summary>Cifra y descifra secretos antes de guardarlos en MongoDB.</summary>
public interface ISecretProtector
{
    /// <summary>True si Security:SecretsKey es una clave válida de 32 bytes en Base64.</summary>
    bool IsConfigured { get; }

    /// <param name="context">Datos asociados (ej. "smtp:Password"): el valor cifrado solo se descifra con el mismo contexto.</param>
    string Protect(string plainText, string context);

    string Unprotect(string protectedValue, string context);
}
