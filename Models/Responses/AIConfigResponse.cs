namespace LoboEsteparioAPI.Models.Responses;

/// <summary>Documento de AIConfig sin secretos: solo se indica qué secretos están definidos.</summary>
public sealed class AIConfigResponse
{
    public required string Id { get; init; }
    public required string Type { get; init; }
    public string? Name { get; init; }
    public required IReadOnlyDictionary<string, string?> Values { get; init; }
    public required IReadOnlyList<string> SecretsSet { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime? UpdatedAtUtc { get; init; }
}
