namespace LoboEsteparioAPI.Options;

/// <summary>Configuración del almacén MongoDB donde se persisten los negocios scrapeados de Overture.</summary>
public sealed class MongoOptions
{
    public const string SectionName = "Mongo";

    public required string ConnectionString { get; set; }
    public required string DatabaseName { get; set; }
    public required string CollectionName { get; set; }
}
