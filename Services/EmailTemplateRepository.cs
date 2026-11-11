using LoboEsteparioAPI.Models.Mongo;
using MongoDB.Bson;
using MongoDB.Driver;

namespace LoboEsteparioAPI.Services;

public sealed class EmailTemplateRepository : IEmailTemplateRepository
{
    private const string CollectionName = "EmailTemplates";
    private readonly IMongoCollection<EmailTemplateDocument> _collection;

    public EmailTemplateRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<EmailTemplateDocument>(CollectionName);
    }

    public async Task<IReadOnlyList<EmailTemplateDocument>> ListAsync(CancellationToken cancellationToken = default)
    {
        await SeedDefaultsIfEmptyAsync(cancellationToken);
        return await _collection.Find(FilterDefinition<EmailTemplateDocument>.Empty)
            .SortByDescending(t => t.IsDefault)
            .ThenByDescending(t => t.UpdatedAtUtc)
            .ThenByDescending(t => t.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<EmailTemplateDocument?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
        await _collection.Find(t => t.Id == id).FirstOrDefaultAsync(cancellationToken);

    public async Task<EmailTemplateDocument> SaveAsync(EmailTemplateDocument template, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(template.Id))
        {
            template.Id = ObjectId.GenerateNewId().ToString();
            template.CreatedAtUtc = DateTime.UtcNow;
            await _collection.InsertOneAsync(template, cancellationToken: cancellationToken);
            return template;
        }

        template.UpdatedAtUtc = DateTime.UtcNow;
        var result = await _collection.ReplaceOneAsync(
            t => t.Id == template.Id,
            template,
            new ReplaceOptions { IsUpsert = true },
            cancellationToken);

        return template;
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var result = await _collection.DeleteOneAsync(t => t.Id == id, cancellationToken);
        return result.DeletedCount > 0;
    }

    public async Task<bool> EnsureAsync(EmailTemplateDocument template, CancellationToken cancellationToken = default)
    {
        var exists = await _collection.Find(t => t.Name == template.Name).AnyAsync(cancellationToken);
        if (exists)
        {
            return false;
        }

        template.Id = ObjectId.GenerateNewId().ToString();
        template.CreatedAtUtc = DateTime.UtcNow;
        await _collection.InsertOneAsync(template, cancellationToken: cancellationToken);
        return true;
    }

    public async Task SeedDefaultsIfEmptyAsync(CancellationToken cancellationToken = default)
    {
        var count = await _collection.CountDocumentsAsync(FilterDefinition<EmailTemplateDocument>.Empty, cancellationToken: cancellationToken);
        if (count > 0)
        {
            return;
        }

        var defaultTemplates = new List<EmailTemplateDocument>
        {
            new()
            {
                Id = ObjectId.GenerateNewId().ToString(),
                Name = "Primer contacto / Presentación B2B",
                Subject = "Oportunidad de colaboración con {Empresa}",
                Body = @"Hola equipo de {Empresa},

Estuve revisando su presencia en el sector de {Categoria} en {Ubicacion} y me pareció muy interesante su propuesta.

Nos especializamos en apoyar a empresas de su sector a optimizar sus procesos y captar nuevos clientes mediante herramientas digitales y estrategias avanzadas.

¿Tendrían disponibilidad para una breve llamada de 10 minutos esta semana para platicar?

Saludos cordiales,
{Remitente}",
                IsDefault = true,
                CreatedAtUtc = DateTime.UtcNow,
            },
            new()
            {
                Id = ObjectId.GenerateNewId().ToString(),
                Name = "Seguimiento cordial",
                Subject = "Seguimiento: colaboración con {Empresa}",
                Body = @"Hola equipo de {Empresa},

Espero que se encuentren muy bien. Les escribo brevemente para dar seguimiento a mi mensaje anterior respecto a posibles oportunidades de sinergia para su negocio en {Ubicacion}.

Entiendo que la agenda suele estar ocupada. Si consideran que este no es el momento oportuno, con gusto podemos agendar para más adelante.

Quedo a su disposición si desean que les compartamos más información.

Atentamente,
{Remitente}",
                IsDefault = true,
                CreatedAtUtc = DateTime.UtcNow,
            },
            new()
            {
                Id = ObjectId.GenerateNewId().ToString(),
                Name = "Propuesta de valor comercial",
                Subject = "Propuesta de valor para {Empresa} ({Categoria})",
                Body = @"Estimado equipo directivo de {Empresa},

Nos dirigimos a ustedes porque hemos identificado oportunidades clave de crecimiento y optimización para empresas del ramo de {Categoria} en {Ubicacion}.

Contamos con soluciones diseñadas específicamente para impulsar el alcance comercial de {Empresa} y maximizar su retorno de inversión.

Nos encantaría compartirles un caso práctico de su industria. ¿A qué correo o número de contacto podríamos coordinar una sesión exploratoria?

Un cordial saludo,
{Remitente}",
                IsDefault = true,
                CreatedAtUtc = DateTime.UtcNow,
            }
        };

        await _collection.InsertManyAsync(defaultTemplates, cancellationToken: cancellationToken);
    }
}
