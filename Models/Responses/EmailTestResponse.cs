namespace LoboEsteparioAPI.Models.Responses;

/// <summary>Resultado del correo de prueba enviado para validar la configuración SMTP.</summary>
public sealed class EmailTestResponse
{
    public required bool Success { get; init; }
    public string? Error { get; init; }
}