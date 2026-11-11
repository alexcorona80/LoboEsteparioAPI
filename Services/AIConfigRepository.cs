using LoboEsteparioAPI.Models.Mongo;
using LoboEsteparioAPI.Models.Requests;
using MongoDB.Driver;

namespace LoboEsteparioAPI.Services;

public sealed class AIConfigRepository(IMongoDatabase database, ISecretProtector protector) : IAIConfigRepository
{
    private const string CollectionName = "AIConfig";
    private readonly IMongoCollection<AIConfigDocument> _collection = database.GetCollection<AIConfigDocument>(CollectionName);

    public const string EmailCopyPromptId = "prompt-email-copy";
    public const string AiProviderId = "ai-provider";
    public const string SmtpId = "smtp";

    public async Task<IReadOnlyList<AIConfigDocument>> ListAsync(CancellationToken cancellationToken = default) =>
        await _collection.Find(FilterDefinition<AIConfigDocument>.Empty)
            .SortBy(d => d.Type)
            .ThenBy(d => d.Id)
            .ToListAsync(cancellationToken);

    public async Task<AIConfigDocument?> GetAsync(string id, CancellationToken cancellationToken = default) =>
        await _collection.Find(d => d.Id == id).FirstOrDefaultAsync(cancellationToken);

    public async Task<AIConfigDocument> UpsertAsync(string id, AIConfigUpsertRequest request, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var document = await GetAsync(id, cancellationToken)
                       ?? new AIConfigDocument { Id = id, Type = request.Type ?? "custom", CreatedAtUtc = now };

        if (!string.IsNullOrWhiteSpace(request.Type))
        {
            document.Type = request.Type.Trim();
        }

        if (request.Name is not null)
        {
            document.Name = request.Name;
        }

        if (request.Values is not null)
        {
            document.Values = new Dictionary<string, string?>(request.Values);
        }

        if (request.Secrets is not null)
        {
            foreach (var (key, value) in request.Secrets)
            {
                if (string.IsNullOrEmpty(value))
                {
                    continue; // vacío = conservar el secreto actual
                }

                document.Secrets[key] = protector.Protect(value, $"{id}:{key}");
            }
        }

        if (request.ClearSecrets is not null)
        {
            foreach (var key in request.ClearSecrets)
            {
                document.Secrets.Remove(key);
            }
        }

        document.UpdatedAtUtc = now;
        await _collection.ReplaceOneAsync(d => d.Id == id, document, new ReplaceOptions { IsUpsert = true }, cancellationToken);
        return document;
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var result = await _collection.DeleteOneAsync(d => d.Id == id, cancellationToken);
        return result.DeletedCount > 0;
    }

    public async Task<string?> GetSecretAsync(string id, string key, CancellationToken cancellationToken = default)
    {
        var document = await GetAsync(id, cancellationToken);
        if (document is null || !document.Secrets.TryGetValue(key, out var cipher))
        {
            return null;
        }

        return protector.Unprotect(cipher, $"{id}:{key}");
    }

    public async Task EnsureSeedAsync(CancellationToken cancellationToken = default)
    {
        foreach (var seed in BuildSeeds())
        {
            var exists = await _collection.Find(d => d.Id == seed.Id).AnyAsync(cancellationToken);
            if (exists)
            {
                continue;
            }

            try
            {
                await _collection.InsertOneAsync(seed, cancellationToken: cancellationToken);
            }
            catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                // Otra instancia lo creó al mismo tiempo.
            }
        }
    }

    private static IEnumerable<AIConfigDocument> BuildSeeds()
    {
        yield return new AIConfigDocument
        {
            Id = EmailCopyPromptId,
            Type = "prompt",
            Name = "Agente de redacción de correos B2B (VETA Supplier)",
            Values = new Dictionary<string, string?>
            {
                ["SystemPrompt"] = EmailCopySystemPrompt,
                ["OutputFormat"] = "json",
                ["Language"] = "es-MX",
            },
        };

        yield return new AIConfigDocument
        {
            Id = AiProviderId,
            Type = "ai_provider",
            Name = "Proveedor de IA",
            Values = new Dictionary<string, string?>
            {
                ["Provider"] = "anthropic",
                ["Model"] = "claude-sonnet-5-5",
                ["ImageModel"] = "",
                ["BaseUrl"] = "",
            },
        };

        yield return new AIConfigDocument
        {
            Id = SmtpId,
            Type = "smtp",
            Name = "Cuenta de correo remitente",
            Values = new Dictionary<string, string?>
            {
                ["Host"] = "",
                ["Port"] = "587",
                ["Username"] = "",
                ["FromName"] = "",
                ["EnableSsl"] = "true",
            },
        };
    }

    private const string EmailCopySystemPrompt = """
        Eres un redactor B2B de prospección en frío para México. Escribes correos breves, respetuosos y útiles que presentan el catálogo de VETA Supplier (empresa mexicana de suministro industrial) a un negocio concreto.

        DATOS DE ENTRADA (JSON): empresa, categoria, ubicacion, remitente.
        - "categoria" es una etiqueta de Overture Maps en inglés y con guion bajo (ej. auto_dealer, restaurant). Interprétala como el giro del negocio y exprésala en español natural.

        CATÁLOGO PERMITIDO (no menciones nada fuera de esta lista):
        - Herramientas: kits de herramientas, accesorios (incluida línea Makita), escaleras de aluminio, lámparas de trabajo recargables, hidrolavadoras de alta presión, polipastos de cadena, estantería industrial, guantes uvex, herramientas antichispa.
        - Refacciones y consumibles: discos de desbaste, cintas aislantes, cinchos de nylon, terminales, puntas para taladro y de torsión, revestimiento anticorrosivo.
        - Control de acceso: lectores biométricos y faciales, torniquetes, chapas magnéticas, barreras vehiculares, bolardos.
        - Maquinaria y logística: montacargas, robots móviles, brazos robot, inspección por rayos X.
        - Cableado: Cat6 y Cat6A, fibra óptica, cable para CCTV y control de acceso, cable fotovoltaico.
        - Servicios: asesoría, instalación, mantenimiento, capacitación y asistencia técnica.

        REGLAS
        1. Elige los 3 o 4 rubros del catálogo más relevantes para el giro de la empresa y ordénalos por relevancia.
        2. No inventes precios, descuentos, plazos de entrega, certificaciones, marcas, cifras ni casos de éxito.
        3. No afirmes conocer la operación de la empresa ni haber trabajado con ella; solo se presenta el catálogo.
        4. Trata de "usted", con tono cordial y profesional. Sin signos de exclamación ni mayúsculas sostenidas.
        5. El texto completo no debe pasar de 120 palabras.
        6. Un único llamado a la acción: solicitar cotización respondiendo el correo.
        7. No incluyas el aviso de baja ni datos de contacto: la plantilla ya los agrega.

        FORMATO DE SALIDA
        Responde únicamente con JSON válido, sin markdown ni texto adicional, con estas claves:
        {
          "asunto": "máx. 60 caracteres",
          "preheader": "máx. 90 caracteres",
          "titular": "máx. 70 caracteres",
          "intro": "1 o 2 frases",
          "puntos": ["3 o 4 elementos de máx. 90 caracteres, cada uno de un rubro del catálogo"],
          "cierre": "1 frase con el llamado a la acción"
        }
        """;
}
