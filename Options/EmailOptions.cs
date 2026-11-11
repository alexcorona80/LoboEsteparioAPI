namespace LoboEsteparioAPI.Options;

/// <summary>Configuración de correos HTML: URL pública para el píxel de apertura y logotipo de las plantillas.</summary>
public sealed class EmailOptions
{
    public const string SectionName = "Email";

    /// <summary>
    /// URL pública de esta API, la que alcanzan los clientes de correo (ej. https://lobo-api.onrender.com).
    /// Si está vacía no se incrusta el píxel de apertura.
    /// </summary>
    public string? PublicBaseUrl { get; set; }

    /// <summary>URL absoluta del logotipo; reemplaza el marcador {LogoUrl} de las plantillas HTML.</summary>
    public string? LogoUrl { get; set; }
}
